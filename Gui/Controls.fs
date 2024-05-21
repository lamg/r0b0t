module Controls

open Gtk

open Types

type DisplayInput =
  { chatDisplay: TextView
    chatInput: TextView }

type StartStop =
  { start: unit -> unit
    stop: unit -> unit }

type InsertWord = string -> unit

type AdjustWord =
  { displayInput: DisplayInput
    adjustment: Adjustment }

type SetProviderModel = Provider -> Model -> unit

type GetQuestion = unit -> string

type ClickSubscriber = (unit -> unit) -> unit

type ShowCommands = unit -> unit

type KeyEventSubscriber = (Gdk.EventKey -> unit) -> unit

// InsertWord

let newAdjustWord (di: DisplayInput) (b: Builder) =
  let adjustment = b.GetObject "text_adjustment" :?> Adjustment

  { displayInput = di
    adjustment = adjustment }

type PngBase64 = string

let insertImage (chatDisplay: TextView) (content: PngBase64) =
  let byteArray = System.Convert.FromBase64String content
  //let p = new Gdk.Pixbuf(byteArray)
  System.IO.File.WriteAllBytes("logo.png", byteArray)
  // Logo for blog "Structured Programming in F#"
  let p = new Gdk.PixbufLoader(byteArray)

  let img = new Image(p.Pixbuf)
  let buff = chatDisplay.Buffer
  let anchor = buff.CreateChildAnchor(ref buff.EndIter)
  chatDisplay.AddChildAtAnchor(img, anchor)

let insertWord ({ displayInput = di; adjustment = adj }: AdjustWord) : InsertWord =
  let f w =
    di.chatDisplay.Buffer.PlaceCursor di.chatDisplay.Buffer.EndIter
    di.chatDisplay.Buffer.InsertAtCursor w
    adj.Value <- adj.Upper

  f

let newGetQuestion (di: DisplayInput) : GetQuestion =
  let f () =
    let question = di.chatInput.Buffer.Text

    di.chatDisplay.Buffer.PlaceCursor di.chatDisplay.Buffer.EndIter
    di.chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
    question

  f

// StartStop

type StartStopInsert =
  { stop: unit -> unit
    start: unit -> unit
    insertWord: InsertWord
    insertImage: PngBase64 -> unit }

let newStopInsert (di: DisplayInput) (builder: Builder) =
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  let adj = newAdjustWord di builder

  { start = answerSpinner.Start
    stop = answerSpinner.Stop
    insertWord = insertImage di.chatDisplay
    insertImage = insertImage di.chatDisplay }

type StopInsert =
  { stop: unit -> unit
    insertWord: string -> unit }

let newAddText (stopInsert: StopInsert) (word: string option) =
  match word with
  | Some w -> stopInsert.insertWord w

  | None ->
    stopInsert.insertWord "\n\n"
    stopInsert.stop ()

type StartAddText =
  { start: unit -> unit
    addText: string option -> unit }

let newStartAddText (ssi: StartStopInsert) =
  { start = ssi.start
    addText =
      newAddText
        { stop = ssi.stop
          insertWord = ssi.insertWord } }

// DisplayInput

let newChatInput (b: Builder) =
  let chatInput = b.GetObject "chat_input" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatInput.StyleContext.AddProvider(provider, 0u)

  chatInput

let newChatDisplay (b: Builder) =
  let chatDisplay = b.GetObject "chat_display" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatDisplay.StyleContext.AddProvider(provider, 0u)
  chatDisplay


let newDisplayInput (b: Builder) =
  { chatDisplay = newChatDisplay b
    chatInput = newChatInput b }


// ProviderLlm

let displayProviderModel (b: Builder) (p: Provider) (m: Model) =
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  providerL.Text <- p
  modelL.Text <- m

let commandList (b: Builder) =
  let commandList = b.GetObject "command_list" :?> ListStore
  commandList.AppendValues [| "bla"; "bli" |] |> ignore
  let tree = b.GetObject "command_view" :?> TreeView
  tree.Model <- commandList

// commands menu

let newShowCommands (b: Builder) =
  let commandsMenu = b.GetObject "commands" :?> PopoverMenu
  commandsMenu.Show: ShowCommands

// subscribers

let confChatInputEvent (chatInput: TextView) =
  (fun f -> chatInput.KeyReleaseEvent.Add(fun k -> f k.Event)): KeyEventSubscriber

let confSend (b: Builder) =
  let send = b.GetObject "send" :?> Button

  (fun f -> send.Clicked.Add(fun _ -> f ())): ClickSubscriber
