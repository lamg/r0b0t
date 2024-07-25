module r0b0tLib.Controls

open Gdk
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

let textBox () =
  let box = new Box()
  box.Homogeneous <- true
  let source = sourceView ()
  source.WrapMode <- WrapMode.Word
  source |> box.Append
  box, source

let onPromptInputKeyRelease (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
  match e.State, e.Keycode with
  | ModifierType.ControlMask, 36ul ->
    // control + enter
    // (sink: RequestProvider)
    printfn "control + enter"
  | _ -> ()


let providerModelBar () =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  box.Hexpand <- true
  box.Homogeneous <- true
  let provider = new Label()
  let model = new Label()
  box.Append provider
  box.Append model
  box, provider, model

let newControls () =
  let interactionBox = new Box()
  interactionBox.SetOrientation Orientation.Horizontal
  interactionBox.Vexpand <- true
  interactionBox.SetHomogeneous true

  let left, leftSrc = textBox ()
  leftSrc.Focusable <- false
  left.Focusable <- false
  leftSrc.Editable <- false
  let picture = new Picture()
  picture.Hide()
  left.Append picture

  let right, rightSrc = textBox ()
  let nav = NavigationHandler()
  let confBox, listBox = configurationBox nav
  confBox.Hide()
  //rightSrc.Hide()
  right.Append confBox

  rightSrc.GrabFocus() |> ignore

  interactionBox.Append left
  interactionBox.Append right

  let topBar, providerLabel, modelLabel = providerModelBar ()
  let windowBox = new Box()
  windowBox.SetOrientation Orientation.Vertical
  windowBox.Append topBar
  windowBox.Append interactionBox

  { leftSrc = leftSrc
    rightSrc = rightSrc
    picture = picture
    confBox = confBox
    rightBox = right
    navigationHandler = nav
    windowBox = windowBox
    listBox = listBox
    providerLabel = providerLabel
    modelLabel = modelLabel }
