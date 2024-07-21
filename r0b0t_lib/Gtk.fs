module r0b0tLib.Gtk

open System
open Gdk
open Gtk

let onSearchChanged (l: ListBox) (_: 'a) (_: EventArgs) = l.InvalidateFilter()

let onFilterInvalidate (s: SearchEntry) (row: ListBoxRow) =
  let box = row.GetChild() :?> Box
  let f = s.GetText().TrimStart().ToLower()
  let r = box.Name.ToLower()
  r.Contains f

let initSearchEntry (s: SearchEntry, l: ListBox) =
  s.add_OnSearchChanged (GObject.SignalHandler<SearchEntry>(onSearchChanged l))

let initListBox (s: SearchEntry, l: ListBox) =
  let cmds =
    [ "Schöneberg"
      "Pankow"
      "Kreuzberg"
      "Wilmersdorf"
      "Steglitz"
      "Tempelhof"
      "Friedrichshain"
      "Köpenick"
      "Tegel"
      "Spandau" ]

  l.ShowSeparators <- true

  for c in cmds do
    let row = new Box()
    row.Name <- c
    row.SetOrientation Orientation.Horizontal
    l.Append row

    let label = new Label()
    label.SetText c
    label.SetHalign Align.Start
    label.SetProperty("hexpand", new GObject.Value(true))
    label.Selectable <- true
    row.Append label

    let image = Image.NewFromIconName "network-workgroup-symbolic"
    image.SetProperty("margin-end", new GObject.Value(5))
    image.SetValign Align.End
    row.Append image

  l.SetFilterFunc(onFilterInvalidate s)

let configurationBox () =
  let box = new Box()
  box.SetOrientation Orientation.Vertical
  box.SetHomogeneous false
  let s = new SearchEntry()
  let l = new ListBox()

  initListBox (s, l)
  initSearchEntry (s, l)
  box.Append s
  box.Append l
  box

let sourceView () =
  GtkSource.Module.Initialize()
  let buf = GtkSource.Buffer.New(null)

  let view = GtkSource.View.NewWithBuffer(buf)
  view.Monospace <- true
  view.ShowLineNumbers <- true
  let m = GtkSource.LanguageManager.New()
  m.GetLanguage("markdown") |> buf.SetLanguage

  view

let textBox () =
  let box = new Box()
  box.Homogeneous <- true
  let source = sourceView ()
  source |> box.Append
  box, source

let onPromptInputKeyRelease (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
  match e.State, e.Keycode with
  | ModifierType.ControlMask, 36ul ->
    // control + enter
    printfn "control + enter"
  | _ ->
     ()
     //printfn $"key {e.Keyval} {e.State} {e.Keycode}"

let onKeyRelease (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
  match e.State, e.Keycode with
  | ModifierType.ControlMask, 27ul ->
    // control + p
    printfn "control + p"
  | ModifierType.NoModifierMask, 9ul ->
    // escape
    printfn "escape"
  | _ ->
     ()
     //printfn $"key {e.Keyval} {e.State} {e.Keycode}"

let mainBox () =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  box.SetHomogeneous true

  let left, leftSrc = textBox ()
  leftSrc.Focusable <- false
  left.Focusable <- false
  leftSrc.Editable <- false

  let right, rightSrc = textBox ()
  let onPromptInputKeyReleaseController = EventControllerKey.New()

  onPromptInputKeyReleaseController.add_OnKeyReleased (
    GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onPromptInputKeyRelease
  )

  rightSrc.AddController onPromptInputKeyReleaseController
  rightSrc.GrabFocus() |> ignore
  
  let onPanelKeyReleaseController = EventControllerKey.New()
  onPanelKeyReleaseController.add_OnKeyReleased (GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onKeyRelease)
  right.AddController onPanelKeyReleaseController
  
  box.Append left
  box.Append right

  box


let onActivateApp (sender: Gio.Application) (_: EventArgs) =
  let window = ApplicationWindow.New(sender :?> Application)

  window.Title <- "r0b0t"
  window.SetDefaultSize(800, 600)
  window.SetChild(mainBox ())
  window.Show()

let main () =
  let application =
    Application.New("com.github.lamg.r0b0t", Gio.ApplicationFlags.FlagsNone)

  application.add_OnActivate (GObject.SignalHandler<Gio.Application>(onActivateApp))
  application.RunWithSynchronizationContext(null)
