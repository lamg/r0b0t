module Core

open FSharp.Control

type Key = Key of string

type Model =
  | Model of string

  override this.ToString() =
    let (Model r) = this
    r

type LlmPrompt = string

type Prompt =
  | LlmPrompt of LlmPrompt
  | Introduction

type PngData =
  { image: byte array
    prompt: string
    revisedPrompt: string }

type LlmData =
  | Word of string
  | PngData of PngData
  | ProgressUpdate of float

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
  abstract member prepare: unit -> unit
  abstract member isBusy: unit -> bool



type ConfigurationManager =
  abstract member storeConfiguration: unit -> unit
  abstract member loadConfiguration: unit -> unit
  abstract member setConfiguration: Configuration -> unit
  abstract member getConfiguration: unit -> Configuration

type CompletionStreamer =
  abstract member streamCompletion: Provider -> Key -> Model -> LlmPrompt -> AsyncSeq<LlmData>

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

let startStream (env: StreamEnv) comp =
  Async.StartWithContinuations(
    computation = comp,
    continuation = env.consumptionEnd,
    exceptionContinuation = env.consumeException,
    cancellationContinuation = ignore
  )

let keyNotFoundMessage provider =
  [ Word $"key not found for {provider}" ] |> AsyncSeq.ofSeq

let setApiKeyMessage provider =
  [ Word $"API key set for {provider}\n" ] |> AsyncSeq.ofSeq

let streamMessage (env: StreamEnv) (message: AsyncSeq<LlmData>) =
  env.prepare ()

  message |> AsyncSeq.iter env.consume |> startStream env

let stream (env: StreamEnv) prompt =
  let conf = env.getConfiguration ()

  match prompt, Map.tryFind conf.provider conf.keys with
  | LlmPrompt prompt, Some key -> env.streamCompletion conf.provider key conf.model prompt
  | LlmPrompt _, None -> keyNotFoundMessage conf.provider
  | Introduction, _ -> welcomeMessage
  |> streamMessage env

let requestProcessor (env: StreamEnv) (r: Request) =
  let conf = env.getConfiguration ()

  match r with
  | SetModel m ->
    let c = { conf with model = m }
    c |> env.setConfiguration
    env.storeConfiguration ()

  | SetProvider p ->
    conf |> setProvider p |> env.setConfiguration
    env.storeConfiguration ()

  | Completion prompt when not (env.isBusy ()) -> stream env prompt
  | SetApiKey(provider, s) ->
    let key = s.Trim()

    env.setConfiguration
      { conf with
          keys = Map.add provider (Key key) conf.keys }

    env.storeConfiguration ()
    streamMessage env (setApiKeyMessage conf.provider)
  | _ -> ()

let plugLogicToEnv (env: StreamEnv) =
  env.loadConfiguration ()
  env.getConfiguration () |> env.setConfiguration
  env.event.Add(requestProcessor env)
