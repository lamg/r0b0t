open Gtk

[<EntryPoint>]
let main _ =
  dotenv.net.DotEnv.Load()
  Application.Init()
  let app = new Application("r0b0t.lamg.github.com", GLib.ApplicationFlags.None)
  app.Register(GLib.Cancellable.Current) |> ignore

  let w = GUI.Main.newWindow ()

  app.AddWindow w
  w.Show()
  Application.Run()
  0
