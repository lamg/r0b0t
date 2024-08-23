module Configuration

open System

type Key = Key of string

type Model =
  | Model of string

  override this.ToString() =
    let (Model r) = this
    r

type Provider =
  | OpenAI
  | GitHub
  | HuggingFace
  | Anthropic
  | ImaginePro

type SerializableConf =
  { provider_keys: Map<string, string>
    model: string
    provider: string }

type Configuration =
  { model: Model
    provider: Provider
    keys: Map<Provider, Key> }

[<Literal>]
let dalle3 = "dall-e-3"

[<Literal>]
let gpt4oMini = "gpt-4o-mini"

let openAIModels = [ gpt4oMini; dalle3; "gpt-4o" ]
let githubModels = [ "copilot" ]
let huggingFaceModels = [ "gpt2" ]

let imagineProAiModels = [ "midjourney" ]

let anthropicModels =
  [ "claude-3-5-sonnet-20240620"
    "claude-3-haiku-20240307"
    "claude-3-opus-20240229" ]

let providersModels =
  [ OpenAI, openAIModels
    Anthropic, anthropicModels
    GitHub, githubModels
    HuggingFace, huggingFaceModels
    ImaginePro, imagineProAiModels ]

let confPath =
  (LamgEnv.getEnv "HOME" |> Option.defaultValue "~")
  :: [ ".config"; "r0b0t.json" ]
  |> List.toArray
  |> System.IO.Path.Join

type ConfigurationManager() =
  let mutable conf =
    { model = Model gpt4oMini
      provider = OpenAI
      keys =
        [ OpenAI, "openai_key"
          Anthropic, "anthropic_key"
          HuggingFace, "huggingface_key"
          GitHub, "github_key"
          ImaginePro, "imaginepro_key" ]
        |> List.choose (fun (p, var) ->
          match LamgEnv.getEnv var with
          | Some k -> Some(p, Key k)
          | _ -> None)
        |> Map.ofList }

  member this.loadConfiguration() =
    if System.IO.File.Exists confPath then
      try
        confPath
        |> IO.File.ReadAllText
        |> Text.Json.JsonSerializer.Deserialize<SerializableConf>
        |> function
          | { model = model
              provider = provider
              provider_keys = pks } ->

            let pks =
              try
                pks.Count |> ignore
                pks
              with _ ->
                Map.empty // a hack to handle the null map, which cannot be handled with Option.ofObj

            let keys =
              providersModels
              |> List.choose (fun (p, _) ->
                match Map.tryFind (p.ToString()) pks with
                | Some key -> Some(p, Key key)
                | None -> None)
              |> Map.ofList

            providersModels
            |> List.tryFind (fun (p, _) -> p.ToString() = provider)
            |> function
              | Some(p, models) when models |> List.exists (fun x -> x = model) ->
                let mergedKeys = conf.keys |> Map.fold (fun m k v -> Map.add k v m) keys

                conf <-
                  { provider = p
                    model = Model model
                    keys = mergedKeys }
              | Some(p, _) -> eprintfn $"model {model} loaded but not supported by {p}"
              | None -> eprintfn $"provider {provider} loaded from configuration, but not supported"
      with e ->
        eprintfn $"failed to load configuration: {e.Message}"
    else
      this.storeConfiguration ()

  member this.storeConfiguration() =
    try
      let (Model model) = conf.model
      let provider = conf.provider.ToString()

      let keys =
        conf.keys
        |> Map.toList
        |> List.map (fun (p, Key k) -> p.ToString(), k)
        |> Map.ofList

      { provider = provider
        model = model
        provider_keys = keys }
      |> (fun v ->
        let opts = Text.Json.JsonSerializerOptions(WriteIndented = true)
        Text.Json.JsonSerializer.Serialize<SerializableConf>(v, opts))
      |> fun json -> IO.File.WriteAllText(confPath, json)
    with e ->
      eprintfn $"failed to store configuration: {e.Message}"

  member _.getConfiguration() = conf

  member _.setConfiguration c = conf <- c
