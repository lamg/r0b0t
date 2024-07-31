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

[<Literal>]
let gpt4oMini = "gpt-4o-mini"

let openAIModels = [ dalle3; "gpt-4o"; gpt4oMini ]
let githubModels = [ "copilot" ]
let huggingFaceModels = [ "gpt2" ]

let anthropicModels =
  [ "claude-3-5-sonnet-20240620"
    "claude-3-haiku-20240307"
    "claude-3-opus-20240229" ]

type ApiKey = string

type Request =
  | SetProvider of Provider
  | SetModel of Model
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
  abstract member consumptionEnd: unit -> unit
  abstract member consumeException: exn -> unit

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
    let comp =
      match Map.tryFind provider conf.keys with
      | Some key -> env.streamCompletion provider key model prompt
      | None -> [ Word $"key not found for {conf.provider}" ] |> AsyncSeq.ofSeq
      |> AsyncSeq.iter env.consume

    Async.StartWithContinuations(
      computation = comp,
      continuation = env.consumptionEnd,
      exceptionContinuation = env.consumeException,
      cancellationContinuation = ignore
    )

  match r with
  | SetModel m ->
    env.storeConfiguration
      { conf with
          model = Model(m.ToString()) }
  | SetProvider p -> env.storeConfiguration { conf with provider = p }
  | Completion prompt -> stream conf.provider conf.model prompt
  | SetApiKey(provider, s) ->
    env.storeConfiguration
      { conf with
          keys = Map.add provider (Key s) conf.keys }
  | Imagine prompt -> stream OpenAI (Model dalle3) prompt

let plugLogicToEnv (env: StreamEnv) =
  env.loadConfiguration () |> env.storeConfiguration
  env.event.Add(requestProcessor env)
