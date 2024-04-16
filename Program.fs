open System
open Gtk
open Types
open OpenAI.ObjectModels

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
  [ openai, Provider.Openai.provider
    github, Provider.Github.provider
    dummy, Provider.Dummy.provider ]
  |> Map.ofList

let getImplementation (provider: string) (model: string) =
  match providerImplementations.TryFind provider, providerToModels.TryFind provider with
  | Some f, Some p -> f p.key model
  | _ -> Provider.Dummy.provider None dummy

[<EntryPoint>]
let main args =
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
