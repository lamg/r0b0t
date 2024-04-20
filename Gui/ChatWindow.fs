module Chat

open System.Threading.Channels
open Gtk
open Types

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

type Components =
  { chatDisplay: TextView
    textAdjusment: Adjustment
    userMessage: Entry
    send: Button
    llmProvider: ComboBoxText
    llm: ComboBoxText
    question: Channel<string>
    fonts: FontButton
    answer: Channel<string option> }

let requestLlmAnswer (provider: string * Channel<string option> -> unit) (c: Components) =
  let addText w _ =
    c.chatDisplay.Buffer.PlaceCursor c.chatDisplay.Buffer.EndIter
    c.chatDisplay.Buffer.InsertAtCursor w
    c.textAdjusment.Value <- c.textAdjusment.Upper
    false

  let rec loop () =
    task {
      let! r = c.answer.Reader.ReadAsync()

      match r with
      | Some w ->
        addText w |> GLib.Idle.Add |> ignore
        return! loop ()
      | None ->
        addText "\n\n" |> GLib.Idle.Add |> ignore
        return ()
    }

  let question = c.userMessage.Text

  c.chatDisplay.Buffer.PlaceCursor c.chatDisplay.Buffer.EndIter
  c.chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "

  provider (question, c.answer)

  loop () |> Async.AwaitTask |> Async.Start


let confChatDisplay (c: Components) =
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  c.chatDisplay.StyleContext.AddProvider(provider, 0u)

let confUserMessage (cfg: Config) (c: Components) =
  c.userMessage.Activated.Add(fun _ ->
    let imp = cfg.implementation c.llmProvider.ActiveText c.llm.ActiveText

    requestLlmAnswer imp c)

let confSend (cfg: Config) (c: Components) =
  c.send.Clicked.Add(fun _ ->
    let imp = cfg.implementation c.llmProvider.ActiveText c.llm.ActiveText

    requestLlmAnswer imp c)

let confLlmProvider (m: Map<string, Provider>) (c: Components) =
  m.Keys |> Seq.iter (fun x -> c.llmProvider.AppendText x)
  c.llmProvider.Active <- 0

let confLlm (m: Map<string, Provider>) (c: Components) =
  let updateModels _ =
    c.llm.RemoveAll()

    m[c.llmProvider.ActiveText].models |> List.iter (fun x -> c.llm.AppendText x)

    c.llm.Active <- 0

  c.llmProvider.Changed.Add updateModels
  updateModels ()

let newChatWindow (cfg: Config) =
  let builder = new Builder("ChatWindow.glade")
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)

  let height = Gdk.Screen.Default.RootWindow.Height
  let width = Gdk.Screen.Default.RootWindow.Width
  window.HeightRequest <- height / 2
  window.WidthRequest <- width / 4

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())
  
  let c =
    { chatDisplay = builder.GetObject "chat_display" :?> TextView
      userMessage = builder.GetObject "user_message" :?> Entry
      send = builder.GetObject "send" :?> Button
      textAdjusment = builder.GetObject "text_adjustment" :?> Adjustment
      llmProvider = builder.GetObject "llm_provider" :?> ComboBoxText
      llm = builder.GetObject "llm" :?> ComboBoxText
      fonts = builder.GetObject "fonts" :?> FontButton
      question = Channel.CreateUnbounded<string>()
      answer = Channel.CreateUnbounded<string option>() }

  confChatDisplay c
  confUserMessage cfg c
  confSend cfg c
  confLlmProvider cfg.provider c
  confLlm cfg.provider c

  window
