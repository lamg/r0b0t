module StreamDeps.GetProvider

open FSharp.Control

type Model = string
type Prompt = string

type ProviderImpl =
  { models: Model list
    answerer: Model -> Prompt -> AsyncSeq<string> }

type Key = string
type KeyEnvVar = string
type Provider = string

type ProviderModule =
  { implementation: Key -> ProviderImpl
    keyVar: KeyEnvVar
    provider: Provider }

type Active = { provider: Provider; model: Model }

type Conf =
  { active: Active
    providers: Map<Provider, ProviderImpl> }

let getenv s =
  System.Environment.GetEnvironmentVariable s |> Option.ofObj

let initConf (xs: ProviderModule list) (_default: Provider) =
  let providers =
    xs
    |> List.choose (fun pm -> getenv pm.keyVar |> Option.map (fun key -> pm.provider, pm.implementation key))
    |> Map.ofList

  let active =
    { provider = _default
      model = providers[_default].models.Head }

  { active = active
    providers = providers }

let getProvider (conf: unit -> Conf) (getPrompt: unit -> Prompt) : Stream.Types.GetProvider =
  fun () ->
    let c = conf ()
    let prompt = getPrompt ()
    c.providers[c.active.provider].answerer c.active.model prompt
