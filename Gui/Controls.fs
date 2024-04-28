module Controls

open Gtk

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

type ProviderLlmSelectors =
  { providerSelector: ComboBoxText
    llmSelector: ComboBoxText
    providerLabel: Label
    modelLabel: Label }

type ClickSubscriber = (unit -> unit) -> unit

type ShowCommands = unit -> unit

type KeyEventSubscriber = (Gdk.EventKey -> unit) -> unit

// InsertWord

let newAdjustWord (di: DisplayInput) (b: Builder) =
  let adjustment = b.GetObject "text_adjustment" :?> Adjustment

  { displayInput = di
    adjustment = adjustment }

let insertWord ({ displayInput = di; adjustment = adj }: AdjustWord) : InsertWord =
  let f w =
    di.chatDisplay.Buffer.PlaceCursor di.chatDisplay.Buffer.EndIter
    di.chatDisplay.Buffer.InsertAtCursor w
    adj.Value <- adj.Upper

  f

// StartStop

type StartStopInsert =
  { stop: unit -> unit
    start: unit -> unit
    insertWord: InsertWord }

let newStopInsert (di: DisplayInput) (builder: Builder) =
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner
  
  let adj = newAdjustWord di builder
  {start = answerSpinner.Start; stop = answerSpinner.Stop; insertWord = insertWord adj}

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

let newProviderLlm (b: Builder) =
  let provider = b.GetObject "llm_provider" :?> ComboBoxText
  let llm = b.GetObject "llm" :?> ComboBoxText
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  { providerSelector = provider
    llmSelector = llm
    providerLabel = providerL
    modelLabel = modelL }


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
