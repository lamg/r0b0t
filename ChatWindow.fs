module Chat

open System
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks

open Gtk

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

type Components =
  { chatDisplay: TextView
    textAdjusment: Adjustment
    userMessage: Entry
    send: Button
    llmProvider: ComboBoxText
    llm: ComboBoxText
    preferences: MenuButton
    question: Channel<string>
    answer: Channel<string option> }

let provideLlmAnswer (question: string, tks: CancellationToken, answer: Channel<string option>) =

  task {
    let xs = [ "bli"; "blo"; "blu"; "coco"; "pepe"; "kiko" ]

    for x in xs do
      do! Task.Delay 1000
      do! answer.Writer.WriteAsync(Some $"{x} ", tks).AsTask()

    do! answer.Writer.WriteAsync None
  }

let requestLlmAnswer (c: Components) _ =
  let tks = new CancellationTokenSource(TimeSpan.FromSeconds 2)

  let addText w =
    c.chatDisplay.Buffer.PlaceCursor c.chatDisplay.Buffer.EndIter
    c.chatDisplay.Buffer.InsertAtCursor w
    c.textAdjusment.Value <- c.textAdjusment.Upper

  let rec loop () =
    task {
      let! r = c.answer.Reader.ReadAsync tks.Token

      match r with
      | Some w ->
        addText w
        return! loop ()
      | None ->
        addText "\n"
        return ()
    }

  task {
    let question = c.userMessage.Text

    c.chatDisplay.Buffer.PlaceCursor c.chatDisplay.Buffer.EndIter
    c.chatDisplay.Buffer.InsertAtCursor $"user: {question}\n🤖:"

    do! provideLlmAnswer (question, tks.Token, c.answer)
    do! loop ()
  }
  |> Async.AwaitTask
  |> Async.Start

let confChatDisplay (c: Components) =
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 16pt; }") |> ignore
  c.chatDisplay.StyleContext.AddProvider(provider, 0u)

let confUserMessage (c: Components) =
  c.userMessage.Activated.Add(requestLlmAnswer c)

let confSend (c: Components) = c.send.Clicked.Add(requestLlmAnswer c)

let providerToModels =
  [ "OpenAI", [ "gpt" ]; "GitHub", [ "copilot" ] ] |> Map.ofList

let confLlmProvider (c: Components) =
  providerToModels.Keys |> Seq.iter (fun x -> c.llmProvider.AppendText x)
  c.llmProvider.Active <- 0

let confLlm (c: Components) =
  let updateModels _ =
    c.llm.RemoveAll()

    providerToModels[c.llmProvider.ActiveText]
    |> List.iter (fun x -> c.llm.AppendText x)

    c.llm.Active <- 0

  c.llmProvider.Changed.Add updateModels
  updateModels ()

let newChatWindow () =
  let builder = new Builder("ChatWindow.glade")
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"


  let window = new ChatWindow(rawWindow)
  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())

  let c =
    { chatDisplay = builder.GetObject "chat_display" :?> TextView
      userMessage = builder.GetObject "user_message" :?> Entry
      send = builder.GetObject "send" :?> Button
      textAdjusment = builder.GetObject "text_adjustment" :?> Adjustment
      llmProvider = builder.GetObject "llm_provider" :?> ComboBoxText
      llm = builder.GetObject "llm" :?> ComboBoxText
      preferences = builder.GetObject "preferences" :?> MenuButton
      question = Channel.CreateUnbounded<string>()
      answer = Channel.CreateUnbounded<string option>() }

  confChatDisplay c
  confUserMessage c
  confSend c
  confLlmProvider c
  confLlm c

  window
