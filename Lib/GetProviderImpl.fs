module GetProviderImpl

open FSharp.Control

type Model = string
type Prompt = string

type ProviderImpl =
  { models: Model list
    answerer: Model -> Prompt -> AsyncSeq<string option> }

type Key = string
type KeyEnvVar = string
type Provider = string

type ProviderModule =
  { implementation: Key -> ProviderImpl
    keyVar: KeyEnvVar
    provider: Provider }

type Active = { provider: Provider; model: Model }

type Conf =
  { mutable active: Active
    mutable providers: Map<Provider, ProviderImpl> }

let getEnv s =
  System.Environment.GetEnvironmentVariable s |> Option.ofObj

let initConf (xs: ProviderModule list) (_default: Provider) =
  let providers =
    xs
    |> List.choose (fun pm -> getEnv pm.keyVar |> Option.map (fun key -> pm.provider, pm.implementation key))
    |> Map.ofList
    |> function
      | m when m.Count = 0 -> failwith "Required environment variable openai_key not defined"
      | m -> m

  let active =
    { provider = _default
      model = providers[_default].models.Head }

  { active = active
    providers = providers }

let newGetProvider (conf: unit -> Conf) (getPrompt: unit -> Prompt) : Stream.Types.GetProvider =
  fun () ->
    let c = conf ()
    let prompt = getPrompt ()
    c.providers[c.active.provider].answerer c.active.model prompt
