module Core

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
  | ImaginePro

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

type ApiKey = string

type Request =
  | SetProvider of Provider
  | SetModel of Model
  | SetApiKey of Provider * ApiKey
  | Completion of Prompt
  | Imagine of Prompt
  | Introduction

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
  abstract member storeConfiguration: unit -> unit
  abstract member loadConfiguration: unit -> unit
  abstract member setConfiguration: Configuration -> unit
  abstract member getConfiguration: unit -> Configuration

type CompletionStreamer =
  abstract member streamCompletion: Provider -> Key -> Model -> Prompt -> AsyncSeq<LlmData>


type StreamEnv =
  inherit RequestProvider
  inherit DataConsumer
  inherit ConfigurationManager
  inherit CompletionStreamer

let setProvider (p: Provider) (conf: Configuration) =
  let model =
    providersModels |> List.find (fun (x, _) -> x = p) |> snd |> List.head |> Model

  { conf with
      provider = p
      model = model }

let welcomeMessage =
  let welcome = "# 🤖 Welcome!\n"
  let showCmdPalette = "- Press **Ctrl+p** for showing the command palette"

  let navigate =
    "- Navigate in the command palette using **Tab**, **Backspace**, **Enter**, **Arrow keys** and **Escape**"

  let sendPrompt = "- Send the prompt to the Language Model using **Ctrl+Enter**\n"

  [ welcome; sendPrompt; showCmdPalette; navigate ]
  |> List.map (fun s -> $"{s}\n".Split " " |> Array.toList)
  |> List.concat
  |> AsyncSeq.ofSeq
  |> AsyncSeq.mapAsync (fun w ->
    async {
      do! Async.Sleep 30
      return Word $"{w} "
    })

let requestProcessor (env: StreamEnv) (r: Request) =
  let conf = env.getConfiguration ()

  let startStream comp =
    Async.StartWithContinuations(
      computation = comp,
      continuation = env.consumptionEnd,
      exceptionContinuation = env.consumeException,
      cancellationContinuation = ignore
    )

  let stream provider model prompt =
    match Map.tryFind provider conf.keys with
    | Some key -> env.streamCompletion provider key model prompt
    | None -> [ Word $"key not found for {conf.provider}" ] |> AsyncSeq.ofSeq
    |> AsyncSeq.iter env.consume
    |> startStream

  match r with
  | SetModel m ->
    let c = { conf with model = m }
    c |> env.setConfiguration
    env.storeConfiguration ()

  | SetProvider p ->
    conf |> setProvider p |> env.setConfiguration
    env.storeConfiguration ()

  | Completion prompt ->
    // TODO ignore request while we are already consuming an answer
    stream conf.provider conf.model prompt
  | SetApiKey(provider, s) ->
    env.setConfiguration
      { conf with
          keys = Map.add provider (Key s) conf.keys }

    env.storeConfiguration ()
  | Imagine prompt -> stream OpenAI (Model dalle3) prompt
  | Introduction -> welcomeMessage |> AsyncSeq.iter env.consume |> startStream

let plugLogicToEnv (env: StreamEnv) =
  env.loadConfiguration ()
  env.getConfiguration () |> env.setConfiguration
  env.event.Add(requestProcessor env)
