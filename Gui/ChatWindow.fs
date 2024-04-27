module Chat

open System.Threading.Channels
open Gtk
open Types

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

type ProviderLlm = { provider: string; llm: string }
type GetProviderLlm = unit -> ProviderLlm

let addText (chatDisplay: TextView) (textAdjustment: Adjustment) (word: string) : GLib.IdleHandler =
  let f () =
    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor word
    textAdjustment.Value <- textAdjustment.Upper
    false

  f

let printQuestion (chatDisplay: TextView) (chatInput: TextView) =
  let question = chatInput.Buffer.Text

  chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
  chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
  question

let readAnswer (answer: Channel<string option>, addText: string -> GLib.IdleHandler) =
  let rec loop () =
    task {
      let! r = answer.Reader.ReadAsync()

      match r with
      | Some w ->
        addText w |> GLib.Idle.Add |> ignore
        return! loop ()
      | None ->
        addText "\n\n" |> GLib.Idle.Add |> ignore
        return ()
    }

  loop () |> Async.AwaitTask |> Async.Start

let confChatDisplay (b: Builder) =
  let chatDisplay = b.GetObject "chat_display" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatDisplay.StyleContext.AddProvider(provider, 0u)
  chatDisplay

let confSelector (providers: Map<string, Provider>) (b: Builder) =
  let llmProvider = b.GetObject "llm_provider" :?> ComboBoxText
  let llm = b.GetObject "llm" :?> ComboBoxText

  fun () ->
    let provider = llmProvider.ActiveText
    let llm = llm.ActiveText
    providers[provider].implementation llm

let confChatInput (b: Builder) =
  let chatInput = b.GetObject "chat_input" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatInput.StyleContext.AddProvider(provider, 0u)

  chatInput


let confSend (b: Builder) (makeQuestion: unit -> unit) =
  let send = b.GetObject "send" :?> Button

  send.Clicked.Add(fun _ -> makeQuestion ())

let confLlm (m: Map<string, Provider>) (b: Builder) =
  let llmProvider = b.GetObject "llm_provider" :?> ComboBoxText
  m.Keys |> Seq.iter (fun x -> llmProvider.AppendText x)
  llmProvider.Active <- 0


  let llm = b.GetObject "llm" :?> ComboBoxText

  let updateModels _ =
    llm.RemoveAll()

    m[llmProvider.ActiveText].models |> List.iter (fun x -> llm.AppendText x)

    llm.Active <- 0

  llmProvider.Changed.Add updateModels
  updateModels ()

let confChatInputEvent (chatInput: TextView) (makeQuestion: unit -> unit) =
  chatInput.KeyReleaseEvent.Add(fun k ->
    if k.Event.Key = Gdk.Key.Return && k.Event.State = Gdk.ModifierType.ControlMask then
      makeQuestion ())

let newChatWindow (providers: Map<string, Provider>) =
  let builder = new Builder("ChatWindow.glade")
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)

  let height = Gdk.Screen.Default.RootWindow.Height
  let width = Gdk.Screen.Default.RootWindow.Width
  window.HeightRequest <- height / 2
  window.WidthRequest <- width / 4

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())

  confLlm providers builder
  let answerStream = Channel.CreateUnbounded<string option>()
  let questionAnswer = confSelector providers builder
  let adjustment = builder.GetObject "text_adjustment" :?> Adjustment

  let chatDisplay = confChatDisplay builder
  let addText = addText chatDisplay adjustment
  let chatInput = confChatInput builder

  let makeQuestion () =
    let question = printQuestion chatDisplay chatInput
    questionAnswer () (question, answerStream)
    readAnswer (answerStream, addText)

  confChatInputEvent chatInput makeQuestion
  confSend builder makeQuestion
  
  window
