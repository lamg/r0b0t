module r0b0tLib.Core

open FSharp.Control

type Key = Key of string
type Model = Model of string
type Prompt = Prompt of string
type Provider = Provider of string

type PngData =
  { image: byte array
    prompt: string
    revisedPrompt: string }

type LlmData =
  | Word of string
  | PngData of PngData

type KeyProvider =
  abstract member key: unit -> Key option

type DataConsumer =
  abstract member consume: LlmData -> unit

type Request =
  | SetModel of Model
  | StreamCompletion of Prompt
  | SetProvider of Provider
  | AvailableModels
  | AvailableProviders

type RequestProvider =
  abstract member event: Event<Request>

type Configuration = { model: Model; provider: Provider }

type ConfigurationManager =
  abstract member storeConfiguration: Configuration -> unit
  abstract member loadConfiguration: unit -> Configuration

type CompletionStreamer =
  abstract member streamCompletion: Key option -> Model -> Prompt -> AsyncSeq<LlmData>

type AvailableModelsProvider =
  abstract member availableModels: unit -> Model list

type AvailableModelsConsumer =
  abstract member consumeAvailableModels: Model list -> unit

type AvailableProvidersProvider =
  abstract member availableProviders : unit -> Provider list
  
type AvailableProviderConsumer =
  abstract member consumeAvailableProviders: Provider list -> unit

type StreamEnv =
  inherit RequestProvider
  inherit KeyProvider
  inherit DataConsumer
  inherit ConfigurationManager
  inherit CompletionStreamer
  inherit AvailableModelsProvider
  inherit AvailableModelsConsumer
  inherit AvailableProvidersProvider
  inherit AvailableProviderConsumer

let requestProcessor (env: StreamEnv) (conf: Configuration) (r: Request) =
  let mutable c = conf

  match r with
  | SetModel m ->
    c <- { c with model = m }
    env.storeConfiguration c
  | SetProvider p ->
    c <- { c with provider = p }
    env.storeConfiguration c
  | StreamCompletion prompt ->
    let key = env.key ()
    let stream = env.streamCompletion key c.model prompt
    stream |> AsyncSeq.iter env.consume |> Async.Start
  | AvailableModels -> env.availableModels () |> env.consumeAvailableModels
  | AvailableProviders -> env.availableProviders () |> env.consumeAvailableProviders

let plugLogicToEnv (env: StreamEnv) =
  let conf = env.loadConfiguration ()
  env.event.Publish.Add(requestProcessor env conf)
