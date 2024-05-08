open System
open Gtk
open Types

open R0b0t

let getenv s =
  Environment.GetEnvironmentVariable s |> Option.ofObj

let initProviders () =
  [ getenv Provider.Openai.environmentVar, Provider.Openai.getProvider
    getenv Provider.Github.environmentVar, Provider.Github.getProvider
    Some "", Provider.Dummy.getProvider ]
  |> List.choose (function
    | Some k, f ->
      let p = f k
      Some(p.name, p)
    | None, _ -> None)
  |> Map.ofList

[<EntryPoint>]
let main _ =
  dotenv.net.DotEnv.Load()

  Application.Init()
  let app = new Application("r0b0t.lamg.github.com", GLib.ApplicationFlags.None)
  app.Register(GLib.Cancellable.Current) |> ignore

  let ps = initProviders ()

  if ps.Keys.Contains Provider.Openai.providerName then
    let win = ChatWindow.newChatWindow ps

    app.AddWindow win
    win.Show()
    Application.Run()
    0
  else
    eprintfn $"Failed to get value of environment variable {Provider.Openai.environmentVar}"
    1
