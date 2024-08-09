module Controls

open Gtk
open CommandPalette

type Controls =
  { leftSrc: GtkSource.View
    picture: Picture
    progress: ProgressBar
    pictureBox: Box
    rightSrc: GtkSource.View
    rightBox: Box
    confBox: Box
    navigationHandler: NavigationHandler
    windowBox: Box
    listBox: ListBox
    searchConf: SearchEntry
    providerLabel: Label
    modelLabel: Label
    spinner: Spinner }

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

let rightPanel () =
  let box = new Box()
  box.Homogeneous <- true
  let source = sourceView ()
  source.AddCssClass "left_text_view"

  let nav = NavigationHandler()
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
  StyleContext.AddProviderForDisplay(Gdk.Display.GetDefault(), css, 1ul)
  scrollable, box, source, confBox, searchConf, listBox, nav

let leftPanel () =
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
  StyleContext.AddProviderForDisplay(Gdk.Display.GetDefault(), css, 1ul)

  scrollable, source, picture, progress, pictureBox


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

  box, provider, model, spinner

let newControls () =
  let interactionBox = new Box()
  interactionBox.SetOrientation Orientation.Horizontal
  interactionBox.Vexpand <- true
  interactionBox.SetHomogeneous true

  let leftScroll, leftSrc, picture, progress, pictureBox = leftPanel ()

  let rightScroll, rightBox, rightSrc, confBox, searchConf, listBox, nav =
    rightPanel ()

  interactionBox.Append leftScroll
  interactionBox.Append rightScroll

  let topBar, providerLabel, modelLabel, spinner = providerModelBar ()

  let windowBox = new Box()
  windowBox.SetOrientation Orientation.Vertical
  windowBox.Append topBar
  windowBox.Append interactionBox

  { leftSrc = leftSrc
    rightSrc = rightSrc
    picture = picture
    progress = progress
    pictureBox = pictureBox
    confBox = confBox
    rightBox = rightBox
    navigationHandler = nav
    windowBox = windowBox
    listBox = listBox
    searchConf = searchConf
    providerLabel = providerLabel
    modelLabel = modelLabel
    spinner = spinner }
