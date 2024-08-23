open System
open Gtk

let onActivateApp (boxes: Core.MainBoxes) (sender: Gio.Application) (_: EventArgs) =
  let window = ApplicationWindow.New(sender :?> Application)
  let css = CssProvider.New()

  css.LoadFromString
    "
.main {
    color: #000000;
    font-weight: normal;
}

.description {
    color: #a8a1a0;
    font-weight: normal;
    font-style: italic;
}"

  StyleContext.AddProviderForDisplay(Gdk.Display.GetDefault(), css, 1ul)

  window.Title <- "r0b0t"
  window.SetDefaultSize(1024, 800)
  let windowBox = new Box()
  windowBox.SetOrientation Orientation.Vertical
  windowBox.Append boxes.topBar

  let interactionBox = new Box()
  interactionBox.SetOrientation Orientation.Horizontal
  interactionBox.Vexpand <- true
  interactionBox.SetHomogeneous true
  interactionBox.Append boxes.leftPanel
  interactionBox.Append boxes.rightPanel
  windowBox.Append interactionBox

  window.SetChild windowBox
  window.Show()
  boxes.init ()

let mainWindow (boxes: Core.MainBoxes) =
  let application =
    Application.New("com.github.lamg.r0b0t", Gio.ApplicationFlags.FlagsNone)

  application.add_OnActivate (GObject.SignalHandler<Gio.Application>(onActivateApp boxes))
  application.RunWithSynchronizationContext(null)


[<EntryPoint>]
let main _ =
  Module.Initialize()
  let boxes = Core.main ()
  mainWindow boxes
