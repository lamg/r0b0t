module GUI.InputOutput

open System
open Gdk
open Gtk

open GetProviderImpl

type InputOutput =
  { getPrompt: unit -> Prompt
    keyRelease: (KeyReleaseEventArgs -> unit) -> unit
    insertWord: string -> unit
    insertImage: string -> unit }

let newConf () =
  let providers =
    [ ProviderModuleImpl.OpenAI.providerModule
      ProviderModuleImpl.GitHub.providerModule ]

  let _default = ProviderModuleImpl.OpenAI.providerModule.provider
  initConf providers _default

type ConfHandler =
  { setConf: Conf -> unit
    getConf: unit -> Conf }

let newConfHandler () =
  let mutable conf = newConf ()
  let setConf (c: Conf) = conf <- c
  let getConf () = conf
  { setConf = setConf; getConf = getConf }

let saveImage (bs: byte array) =
  let now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss")
  IO.File.WriteAllBytes($"img_{now}.png", bs)

let insertImage (insertWord: string -> unit) (chatDisplay: TextView) (content: string) =
  let bs = Convert.FromBase64String content
  saveImage bs
  let p = new PixbufLoader(bs)
  let height = chatDisplay.AllocatedHeight / 2
  let width = chatDisplay.AllocatedWidth / 2
  let np = p.Pixbuf.ScaleSimple(height, width, InterpType.Bilinear)
  let buff = chatDisplay.Buffer

  GLib.Idle.Add(fun _ ->
    buff.InsertPixbuf(ref buff.EndIter, np)
    false)
  |> ignore

  insertWord ""


let getPrompt (chatInput: TextView, chatDisplay: TextView) =
  fun () ->
    let question = chatInput.Buffer.Text

    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
    question

let insertWord (chatDisplay: TextView, adjustment: Adjustment) (w: string) =
  let word =
    match w with
    | w when w = String.Empty || isNull w -> "\n\n"
    | w -> w

  GLib.Idle.Add(fun _ ->
    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor word
    adjustment.Value <- adjustment.Upper
    false)
  |> ignore

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

let newWindow (builder: Builder) =
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)
  window.Maximize()
  window.Title <- "r0b0t"

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())
  window

let displayProviderModel (b: Builder) (a: Active) =
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  providerL.Text <- a.provider
  modelL.Text <- a.model

let inspectCombo (combo: ComboBoxText) =
  let listStore = combo.Model :?> ListStore
  let mutable iter = TreeIter()
  let mutable isValid = listStore.GetIterFirst(&iter) // Get the first item
  // Iterate through all items
  while isValid do
    let value = listStore.GetValue(iter, 0) :?> string
    printfn $"Item: {value}"
    isValid <- listStore.IterNext(&iter)

let settingBox (name: string) (active: string) (values: string seq) (changed: string -> unit) =
  let box = new Box(Orientation.Horizontal, 2)
  let label = new Label(name)
  label.CanFocus <- false
  box.PackStart(label, false, true, 5u)
  let combo = new ComboBoxText()
  values |> Seq.iter combo.AppendText
  let eq x y = x = y
  combo.Active <- values |> Seq.findIndex (eq active)
  box.PackStart(combo, true, true, 0u)
  combo.Changed.Add(fun _ -> changed combo.ActiveText)

  box,
  (fun (active: string) (items: string list) ->
    combo.RemoveAll()
    items |> Seq.iter combo.AppendText
    let activeIndex = items |> Seq.findIndex (eq active)
    combo.Active <- activeIndex)

let changedModel (modelLabel: Label) ch active =
  let conf = ch.getConf ()

  if validModelState conf active then
    setActiveModel conf active |> ch.setConf
    modelLabel.Text <- active

let changedProvider (providerLabel: Label) (modelLabel: Label) ch updateModels active =
  let conf = ch.getConf ()

  if validProviderState conf active then
    setActiveProvider conf active |> ch.setConf
    let conf = ch.getConf ()
    updateModels conf.active.model conf.providers[conf.active.provider].models
    providerLabel.Text <- conf.active.provider
    modelLabel.Text <- conf.active.model


let newShowCommands (ch: ConfHandler) (b: Builder) =
  let commandList = b.GetObject "command_list" :?> ListBox
  let commandSearch = b.GetObject "command_search" :?> SearchEntry
  let commandBox = b.GetObject "command_box" :?> Box
  let providerLabel = b.GetObject "provider_label" :?> Label
  let modelLabel = b.GetObject "model_label" :?> Label

  let conf = ch.getConf ()

  let modelSetting, updateModels =
    settingBox "Models" conf.active.model conf.providers[conf.active.provider].models (changedModel modelLabel ch)

  let providerSetting, _ =
    let conf = ch.getConf ()

    settingBox
      "Providers"
      conf.active.provider
      conf.providers.Keys
      (changedProvider providerLabel modelLabel ch updateModels)

  commandList.Add modelSetting
  commandList.Add providerSetting

  commandBox.Expand <- true
  commandList.ShowAll()

  fun () ->
    commandSearch.GrabFocus()
    commandBox.Visible <- true


let newHideCommands (b: Builder) =
  let commandBox = b.GetObject "command_box" :?> Box
  let chatInput = b.GetObject "chat_input" :?> TextView

  fun () ->
    chatInput.GrabFocus()
    commandBox.Visible <- false


let newInputOutput (w: Window) (b: Builder) : InputOutput =
  let chatDisplay = b.GetObject "chat_display" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatDisplay.StyleContext.AddProvider(provider, 0u)

  let chatInput = b.GetObject "chat_input" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatInput.StyleContext.AddProvider(provider, 0u)
  chatInput.GrabFocus()

  let adjustment = b.GetObject "text_adjustment" :?> Adjustment
  let insertWord = insertWord (chatDisplay, adjustment)

  { getPrompt = getPrompt (chatInput, chatDisplay)
    keyRelease = w.KeyReleaseEvent.Add
    insertWord = insertWord
    insertImage = insertImage insertWord chatDisplay }
