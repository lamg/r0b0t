open System
open Gtk
open r0b0tLib

let onActivateApp (box: Box) (sender: Gio.Application) (_: EventArgs) =
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
  window.SetDefaultSize(800, 600)
  window.SetChild box
  window.Show()

let mainWindow (mainBox: Box) =
  let application =
    Application.New("com.github.lamg.r0b0t", Gio.ApplicationFlags.FlagsNone)

  application.add_OnActivate (GObject.SignalHandler<Gio.Application>(onActivateApp mainBox))
  application.RunWithSynchronizationContext(null)


[<EntryPoint>]
let main _ =
  Module.Initialize()
  let controls = Controls.newControls ()
  controls |> StreamEnvProvider.newStreamEnv |> Core.plugLogicToEnv
  mainWindow controls.windowBox
