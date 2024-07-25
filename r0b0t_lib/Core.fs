module r0b0tLib.Core

open FSharp.Control

type Key = Key of string

type Model =
  | Model of string

  override this.ToString() =
    let (Model r) = this
    r

type Prompt = Prompt of string

type PngData =
  { image: byte array
    prompt: string
    revisedPrompt: string }

type LlmData =
  | Word of string
  | PngData of PngData

type Provider =
  | OpenAI
  | GitHub
  | HuggingFace
  | Anthropic

[<Literal>]
let dalle3 = "dall-e-3"

type OpenAIModel =
  | Gpt4o
  | Gpt4oMini
  | Dalle3

  override this.ToString() =
    match this with
    | Gpt4o -> "gpt-4o"
    | Gpt4oMini -> "gpt-4o-mini"
    | Dalle3 -> dalle3

type GitHubModel = Copilot
type HuggingFaceModel = Gpt2

type AnthropicModel =
  | Sonnet35
  | Haiku3
  | Opus3

  override this.ToString() =
    match this with
    | Sonnet35 -> "claude-3-5-sonnet"
    | Haiku3 -> "claude-3-haiku"
    | Opus3 -> "claude-3-opus"

type ApiKey = string

type Request =
  | SetProvider of Provider
  | SetOpenAIModel of OpenAIModel
  | SetAnthropicModel of AnthropicModel
  | SetApiKey of Provider * ApiKey
  | Completion of Prompt
  | Imagine of Prompt


type Configuration =
  { model: Model
    provider: Provider
    keys: Map<Provider, Key> }

type RequestProvider =
  abstract member event: IEvent<Request>

type DataConsumer =
  abstract member consume: LlmData -> unit

type ConfigurationManager =
  abstract member storeConfiguration: Configuration -> unit
  abstract member loadConfiguration: unit -> Configuration

type CompletionStreamer =
  abstract member streamCompletion: Provider -> Key -> Model -> Prompt -> AsyncSeq<LlmData>

type StreamEnv =
  inherit RequestProvider
  inherit DataConsumer
  inherit ConfigurationManager
  inherit CompletionStreamer

let requestProcessor (env: StreamEnv) (r: Request) =
  let conf = env.loadConfiguration ()

  let stream provider model prompt =
    match Map.tryFind provider conf.keys with
    | Some key -> env.streamCompletion provider key model prompt
    | None -> [ Word $"key not found for {conf.provider}" ] |> AsyncSeq.ofSeq
    |> AsyncSeq.iter env.consume
    |> Async.Start

  match r with
  | SetAnthropicModel m ->
    env.storeConfiguration
      { conf with
          model = Model(m.ToString()) }
  | SetOpenAIModel m ->
    env.storeConfiguration
      { conf with
          model = Model(m.ToString()) }
  | SetProvider p -> env.storeConfiguration { conf with provider = p }
  | Completion prompt -> stream conf.provider conf.model prompt
  | SetApiKey(provider, s) ->
    env.storeConfiguration
      { conf with
          keys = Map.add provider (Key s) conf.keys }
  | Imagine prompt -> stream OpenAI (Model(Dalle3.ToString())) prompt

let plugLogicToEnv (env: StreamEnv) = env.event.Add(requestProcessor env)
