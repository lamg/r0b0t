open System
open Gtk
open Types
open OpenAI.ObjectModels

open R0b0t

let getenv s =
  Environment.GetEnvironmentVariable s |> Option.ofObj

let initProviders () =
  [ getenv Provider.Openai.environmentVar, Provider.Openai.getProvider
    getenv Provider.Github.environmentVar, Provider.Github.getProvider ]
  |> List.choose (function
    | Some k, f ->
      let p = f k
      Some(p.name, p)
    | None, _ -> None)
  |> Map.ofList

[<EntryPoint>]
let main _ =
  Application.Init()
  let app = new Application("r0b0t.lamg.github.com", GLib.ApplicationFlags.None)
  app.Register(GLib.Cancellable.Current) |> ignore

  let ps = initProviders ()
  let win = Chat.newChatWindow ps

  app.AddWindow(win)
  win.Show()
  Application.Run()
  0
