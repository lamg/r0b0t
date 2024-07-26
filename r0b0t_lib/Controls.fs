module r0b0tLib.Controls

open Gtk
open r0b0tLib.CommandPalette

type Controls =
  { leftSrc: GtkSource.View
    picture: Picture
    rightSrc: GtkSource.View
    rightBox: Box
    confBox: Box
    navigationHandler: NavigationHandler
    windowBox: Box
    listBox: ListBox
    providerLabel: Label
    modelLabel: Label }

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
  let confBox, listBox = configurationBox nav
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

  scrollable, box, source, confBox, listBox, nav

let leftPanel () =
  let box = new Box()
  box.Homogeneous <- true
  let source = sourceView ()
  source.AddCssClass "left_text_view"
  source.Focusable <- false
  box.Focusable <- false
  source.Editable <- false
  let picture = new Picture()
  picture.Hide()
  box.Append source
  box.Append picture
  let scrollable = new ScrolledWindow()
  scrollable.SetChild box

  let css = CssProvider.New()
  let body = "font-family: \"Monaspace Krypton\"; font-size: 16pt;"
  let style = ".left_text_view {" + body + "}"
  css.LoadFromString style
  StyleContext.AddProviderForDisplay(Gdk.Display.GetDefault(), css, 1ul)

  scrollable, source, picture


let providerModelBar () =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  box.Hexpand <- true
  box.Homogeneous <- false
  let provider = new Label()
  provider.Hexpand <- true
  let model = new Label()
  model.Hexpand <- true

  box.Append provider
  box.Append model

  box, provider, model

let newControls () =
  let interactionBox = new Box()
  interactionBox.SetOrientation Orientation.Horizontal
  interactionBox.Vexpand <- true
  interactionBox.SetHomogeneous true

  let leftScroll, leftSrc, picture = leftPanel ()
  let rightScroll, rightBox, rightSrc, confBox, listBox, nav = rightPanel ()

  interactionBox.Append leftScroll
  interactionBox.Append rightScroll

  let topBar, providerLabel, modelLabel = providerModelBar ()

  let windowBox = new Box()
  windowBox.SetOrientation Orientation.Vertical
  windowBox.Append topBar
  windowBox.Append interactionBox

  { leftSrc = leftSrc
    rightSrc = rightSrc
    picture = picture
    confBox = confBox
    rightBox = rightBox
    navigationHandler = nav
    windowBox = windowBox
    listBox = listBox
    providerLabel = providerLabel
    modelLabel = modelLabel }
