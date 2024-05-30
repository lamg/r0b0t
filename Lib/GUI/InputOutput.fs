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

let newShowCommands (conf: Conf) (b: Builder) =
  let commandList = b.GetObject "command_list" :?> ListBox
  let commandSearch = b.GetObject "command_search" :?> SearchEntry
  let commandBox = b.GetObject "command_box" :?> Box

  let providersRow = new Box(Orientation.Horizontal, 2)
  providersRow.PackStart(new Label("Providers"), false, true, 5u)
  let providers = new ComboBoxText()
  conf.providers.Keys |> Seq.iter providers.AppendText
  providersRow.PackStart(providers, true, true, 0u)
  commandList.Add providersRow

  let modelsRow = new Box(Orientation.Horizontal, 2)
  modelsRow.PackStart(new Label("Models"), false, true, 5u)
  let models = new ComboBoxText()
  conf.providers[conf.active.provider].models |> Seq.iter models.AppendText
  modelsRow.PackStart(models, true, true, 0u)
  commandList.Add modelsRow
  models.Active <- 0

  commandBox.Expand <- true
  commandList.ShowAll()

  let providerLabel = b.GetObject "provider_label" :?> Label
  let modelLabel = b.GetObject "model_label" :?> Label

  providers.Changed.Add(fun _ ->
    conf.active <-
      { provider = providers.ActiveText
        model = conf.providers[providers.ActiveText].models.Head }

    models.Clear()
    conf.providers[conf.active.provider].models |> Seq.iter models.AppendText
    providerLabel.Text <- conf.active.provider
    modelLabel.Text <- conf.active.model)

  models.Changed.Add(fun _ ->
    conf.active <-
      { conf.active with
          model = models.ActiveText }

    modelLabel.Text <- conf.active.model)

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
