module GtkGui

open System

open Gtk
open Gdk
open GdkPixbuf
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png.Chunks

open Navigation

type PngData =
  { image: byte array
    prompt: string
    revisedPrompt: string }

type LlmData =
  | Prepare
  | Word of string
  | PngData of PngData
  | ProgressUpdate of float
  | StreamExc of exn
  | End

[<Literal>]
let promptPngMetadataKey = "prompt"

[<Literal>]
let revisedPromptPngMetadataKey = "revised_prompt"

let controlEnter = ModifierType.ControlMask, 36ul
let controlP = ModifierType.ControlMask, 27ul
let escape = ModifierType.NoModifierMask, 9ul
let backspace = ModifierType.NoModifierMask, 22ul
let downArrow = ModifierType.NoModifierMask, 116ul

let settingLabel (text: string) =
  let l = new Label()
  l.SetText text
  l.SetHalign Align.Start
  l.SetValign Align.Start
  l.SetHexpand true
  l.SetVexpand true
  l.SetWrap true
  l.SetMarginBottom 10
  l.AddCssClass "setting-label"
  l.Focusable <- false
  l

let boolSetting text value =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  settingLabel text |> box.Append
  let check = new CheckButton()
  check.Active <- value
  check |> box.Append

  box

let stringSetting text value =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  settingLabel text |> box.Append
  let entry = new Entry()
  entry.Text_ <- value
  entry |> box.Append
  entry.GrabFocus() |> ignore
  box

let settingGroup (label, description) =
  let mainLabel = new Label()
  mainLabel.SetText label
  mainLabel.SetHalign Align.Start
  mainLabel.SetValign Align.Start
  mainLabel.SetHexpand true
  mainLabel.SetVexpand true
  mainLabel.SetWrap true
  mainLabel.SetMarginBottom 10
  mainLabel.AddCssClass "main"

  let descrLabel = new Label()
  descrLabel.SetText description
  descrLabel.SetEllipsize Pango.EllipsizeMode.End
  descrLabel.SetHalign Align.Start
  descrLabel.SetValign Align.End
  descrLabel.SetHexpand true
  descrLabel.SetVexpand false
  descrLabel.SetMarginBottom 10
  descrLabel.AddCssClass "description"

  let item = new Box()
  item.Name <- label

  item.SetOrientation Orientation.Vertical
  item.Append mainLabel
  item.Append descrLabel
  item

let populateListBox (l: ListBox) (xs: Control array) =
  let appendRow =
    function
    | Checkbox(label, v) -> boolSetting label v
    | Entry(label, text) -> stringSetting label text
    | Group(name, descr) -> settingGroup (name, descr)
    >> l.Append

  l.RemoveAll()

  xs |> Seq.iter appendRow

let sourceView () =
  GtkSource.Module.Initialize()
  let buf = GtkSource.Buffer.New(null)

  let view = GtkSource.View.NewWithBuffer(buf)
  view.Monospace <- true
  view.ShowLineNumbers <- true
  view.AddCssClass "text_view"
  view.WrapMode <- WrapMode.Word
  let m = GtkSource.LanguageManager.New()
  m.GetLanguage("markdown") |> buf.SetLanguage
  let settings = Settings.GetDefault()

  let shouldUseDark =
    settings.GtkApplicationPreferDarkTheme
    || settings.GtkThemeName.ToLower().Contains "dark"
  // TODO fix theming
  // printfn $"{settings.GtkApplicationPreferDarkTheme} {settings.GtkThemeName}"
  if shouldUseDark then
    buf.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme("Adwaita-dark"))

  view

let configurationBox (nav: NavigationHandler) =
  let box = new Box()
  box.SetOrientation Orientation.Vertical
  box.SetHomogeneous false
  let search = new SearchEntry()
  let list = new ListBox()

  let updateList () =
    //nav.setNameFilter search.Text_
    nav.activateAndGetSettings (None, None) |> populateListBox list

  let onSearchChanged _ _ = updateList ()

  search.add_OnSearchChanged (GObject.SignalHandler<SearchEntry> onSearchChanged)
  updateList ()

  let onActivateItem (lb: ListBox) (e: ListBox.RowActivatedSignalArgs) =
    let index = e.Row.GetIndex()

    let text =
      match e.Row.GetLastChild() :?> Box |> _.GetLastChild() with
      | :? Entry as e -> Some e.Text_
      | _ -> None

    nav.activateAndGetSettings (Some index, text) |> populateListBox list


  list.add_OnRowActivated (GObject.SignalHandler<ListBox, ListBox.RowActivatedSignalArgs> onActivateItem)

  box.Append search
  box.Append list
  box, search, list

let providerModelBar () =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  box.Hexpand <- true
  box.Homogeneous <- false
  let provider = new Label()
  provider.Hexpand <- true
  let model = new Label()
  model.Hexpand <- true
  let spinner = new Spinner()
  box.Append provider
  box.Append model
  box.Append spinner

  {| box = box
     provider = provider
     model = model
     spinner = spinner |}


let rightPanel (nav: NavigationHandler) =
  let box = new Box()
  box.Homogeneous <- true
  let source = sourceView ()
  let ctrlEnterController = EventControllerKey.New()

  source.AddController ctrlEnterController
  source.AddCssClass "left_text_view"

  let confBox, searchConf, listBox = configurationBox nav
  confBox.Hide()
  box.Append confBox
  box.Append source
  let scrollable = new ScrolledWindow()
  scrollable.SetChild box

  let css = CssProvider.New()
  let body = "font-family: \"Cascadia Code\"; font-size: 16pt;"
  let style = ".left_text_view {" + body + "}"
  css.LoadFromString style
  StyleContext.AddProviderForDisplay(Display.GetDefault(), css, 1ul)

  let topBar = providerModelBar ()

  let onNavigation (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    let keys = e.State, e.Keycode

    match keys with
    | _ when keys = escape ->
      confBox.Hide()
      source.Show()
      source.GrabFocus() |> ignore
    | _ when keys = controlP -> searchConf.GrabFocus() |> ignore
    | _ when keys = backspace -> nav.backToRoot () |> populateListBox listBox
    | _ when keys = downArrow && searchConf.HasFocus -> listBox.GetFirstChild().GrabFocus() |> ignore
    | _ -> ()

  let onCtrlEnterSendPrompt (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    let keys = e.State, e.Keycode

    match keys with
    | _ when keys = controlEnter -> nav.completion source.Buffer.Text
    | _ when keys = controlP ->
      // control + p
      source.Hide()
      confBox.Show()
      searchConf.GrabFocus() |> ignore
    | _ -> ()

  ctrlEnterController.add_OnKeyReleased (
    GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onCtrlEnterSendPrompt
  )

  let navController = EventControllerKey.New()

  navController.add_OnKeyReleased (
    GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onNavigation
  )

  confBox.AddController navController

  {| rightPanel = scrollable
     topBar = topBar |}

let leftPanel (spinner: Spinner) (onData: IEvent<LlmData>) =
  let box = new Box()
  box.SetOrientation Orientation.Vertical
  box.Homogeneous <- true
  let source = sourceView ()
  source.AddCssClass "left_text_view"
  source.Focusable <- false
  box.Focusable <- false
  source.Editable <- false

  let picture = new Picture()
  picture.Hexpand <- true
  picture.Vexpand <- true

  let progress = new ProgressBar()
  progress.Hexpand <- true
  progress.Vexpand <- false

  let pictureBox = new Box()
  pictureBox.SetOrientation Orientation.Vertical
  pictureBox.Homogeneous <- false
  pictureBox.Append progress
  pictureBox.Append picture
  pictureBox.Hide()

  box.Append source
  box.Append pictureBox

  let scrollable = new ScrolledWindow()
  scrollable.SetChild box

  let css = CssProvider.New()
  let body = "font-family: \"Monaspace Krypton\"; font-size: 16pt;"
  let style = ".left_text_view {" + body + "}"
  css.LoadFromString style
  StyleContext.AddProviderForDisplay(Display.GetDefault(), css, 1ul)

  let saveImage (d: PngData) =
    use image = Image.Load d.image
    let pngMeta = image.Metadata.GetPngMetadata()
    pngMeta.TextData.Add(PngTextData(promptPngMetadataKey, d.prompt, "en", promptPngMetadataKey))
    pngMeta.TextData.Add(PngTextData(revisedPromptPngMetadataKey, d.revisedPrompt, "en", revisedPromptPngMetadataKey))
    let now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss")
    let outputPath = $"img_{now}.png"
    image.Save(outputPath)

  let consume (d: LlmData) =
    let appendText w =
      if not source.Visible then
        source.Show()
        pictureBox.Hide()

      source.Buffer.Text <- $"{source.Buffer.Text}{w}"

    GLib.Functions.IdleAdd(
      int GLib.ThreadPriority.Normal,
      fun _ ->
        match d with
        | Prepare ->
          spinner.Start()
          source.Buffer.Text <- ""
        | Word w -> appendText w

        | PngData img ->
          if not pictureBox.Visible then

            source.Hide()
            pictureBox.Show()

          let p = PixbufLoader.FromBytes img.image
          picture.SetPixbuf p
          saveImage img
        | ProgressUpdate percent ->
          if not pictureBox.Visible then
            source.Hide()
            pictureBox.Show()

          progress.SetFraction percent
        | End -> spinner.Stop()
        | StreamExc e -> appendText e.Message

        false
    )
    |> ignore

  onData.Add consume
  scrollable
