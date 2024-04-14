open System
open Gtk

[<EntryPoint>]
let main args =
  Application.Init()
  let app = new Application("r0b0t.lamg.github.com", GLib.ApplicationFlags.None)
  app.Register(GLib.Cancellable.Current) |> ignore
  let win = Chat.newChatWindow ()
  app.AddWindow(win)
  win.Show()
  Application.Run()
  0
