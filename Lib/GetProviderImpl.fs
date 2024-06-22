module GetProviderImpl

open FSharp.Control
open Stream.Types
open LamgEnv

type Model = string
type Prompt = string

type ProviderImpl =
  { models: Model list
    answerer: Model -> Prompt -> AsyncSeq<LlmData option>
    _default: Model }

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
    path: string option
    providers: Map<Provider, ProviderImpl> }

let saveActive (confPath: string, a: Active) =
  System.IO.File.WriteAllText(confPath, System.Text.Json.JsonSerializer.Serialize a)

let loadActive (_default: Active) =
  let deserializeActive (json: string) : Active =
    System.Text.Json.JsonSerializer.Deserialize<Active>(json)

  match getEnv "HOME" with
  | Some home ->
    let confPath =
      home :: [ ".config"; "r0b0t.json" ] |> List.toArray |> System.IO.Path.Join

    let active =
      if System.IO.File.Exists confPath then
        System.IO.File.ReadAllText confPath |> deserializeActive
      else
        saveActive (confPath, _default)
        _default

    Some confPath, active
  | None -> None, _default

let initConf (xs: ProviderModule list) (_default: Provider) =
  let providers =
    xs
    |> List.choose (fun pm -> getEnv pm.keyVar |> Option.map (fun key -> pm.provider, pm.implementation key))
    |> Map.ofList
    |> function
      | m when m.Count = 0 -> failwith "Required environment variable openai_key not defined"
      | m -> m

  let confPath, active =
    { provider = _default
      model = providers[_default]._default }
    |> loadActive

  { active = active
    path = confPath
    providers = providers }

let newGetProvider (c: Conf) (getPrompt: unit -> Prompt) : GetProvider =
  fun () ->
    let prompt = getPrompt ()
    c.providers[c.active.provider].answerer c.active.model prompt

let validProviderState (conf: Conf) (p: Provider) = conf.providers.ContainsKey p

let validModelState (conf: Conf) (m: Model) =
  validProviderState conf conf.active.provider
  && (conf.providers[conf.active.provider].models |> Seq.contains m)

let setActiveProvider (conf: Conf) (p: Provider) =
  if validProviderState conf p then
    let active =
      { provider = p
        model = conf.providers[p]._default }

    conf.path |> Option.iter (fun path -> saveActive (path, conf.active))
    { conf with active = active }
  else
    failwith $"Provider '{p}' not found"

let idIter (f: 'a -> unit) (x: 'a) =
  f x
  x

let setActiveModel (conf: Conf) (m: Model) =
  if validModelState conf m then
    { conf with active.model = m }
    |> idIter (fun c -> c.path |> Option.iter (fun path -> saveActive (path, c.active)))
  else
    failwith $"unknown model '{m}' for provider '{conf.active.provider}'"
