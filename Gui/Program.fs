open System
open Gtk
open Types
open OpenAI.ObjectModels

open R0b0t

let getenv s =
  Environment.GetEnvironmentVariable s |> Option.ofObj

[<Literal>]
let github = "GitHub"

[<Literal>]
let openai = "OpenAI"

[<Literal>]
let dummy = "Dummy"

[<Literal>]
let openaiKey = "openai_key"

[<Literal>]
let githubKey = "github_key"

let providerToModels =
  dotenv.net.DotEnv.Load()

  [ openai, openaiKey, [ Models.Gpt_3_5_Turbo; Models.Gpt_4; Models.Gpt_3_5_Turbo_16k; Models.Gpt_4_turbo ]
    github, githubKey, [ "Copilot" ]
    dummy, "dummy_key", [ "dummy" ] ]
  |> List.map (fun (provider, envVar, models) ->
    provider,
    { name = provider
      envVar = envVar
      key = getenv envVar
      models = models })
  |> Map.ofList

let providerImplementations =
  [ openai, Provider.Openai.ask
    github, Provider.Github.ask
    dummy, Provider.Dummy.provider ]
  |> Map.ofList

let getImplementation (provider: string) (model: string) =
  match providerImplementations.TryFind provider, providerToModels.TryFind provider with
  | Some f, Some { key = Some k } -> f k model
  | Some _, Some { key = None } -> (fun (_, answer) -> Provider.Util.sendAnswer ["not"; "found"; "key"; "for"; provider] answer)
  | _ -> Provider.Dummy.provider "" ""

[<EntryPoint>]
let main _ =
  Application.Init()
  let app = new Application("r0b0t.lamg.github.com", GLib.ApplicationFlags.None)
  app.Register(GLib.Cancellable.Current) |> ignore

  let win =
    Chat.newChatWindow
      { provider = providerToModels
        implementation = getImplementation }

  app.AddWindow(win)
  win.Show()
  Application.Run()
  0
