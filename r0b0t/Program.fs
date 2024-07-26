open System
open Gtk
open r0b0tLib

let onActivateApp (controls: Controls.Controls) (sender: Gio.Application) (_: EventArgs) =
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
  window.SetChild controls.windowBox
  window.Show()
  controls.rightSrc.GrabFocus() |> ignore

let mainWindow (controls: Controls.Controls) =
  let application =
    Application.New("com.github.lamg.r0b0t", Gio.ApplicationFlags.FlagsNone)

  application.add_OnActivate (GObject.SignalHandler<Gio.Application>(onActivateApp controls))
  application.RunWithSynchronizationContext(null)


[<EntryPoint>]
let main _ =
  Module.Initialize()
  let controls = Controls.newControls ()
  controls |> StreamEnvProvider.newStreamEnv |> Core.plugLogicToEnv
  mainWindow controls
