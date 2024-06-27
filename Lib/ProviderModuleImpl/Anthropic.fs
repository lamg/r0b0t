module ProviderModuleImpl.Anthropic

open FSharp.Control
open FsHttp
open GetProviderImpl
open ServerSentEvents
open Stream.Types

let claude21 = "claude-2.1"
let claude2 = "claude-2.0"
let claudeInstant12 = "claude-instant-1.2"
let haiku3 = GetProviderImpl.Model "claude-3-haiku-20240307"
let opus3 = GetProviderImpl.Model "claude-3-opus-20240229"
let sonnet3 = GetProviderImpl.Model "claude-3-sonnet-20240229"
let sonnet35 = GetProviderImpl.Model "claude-3-5-sonnet-20240620"

type Delta =
  { ``type``: string option
    text: string option }

type Message =
  { ``type``: string
    message: obj option
    content_block: obj option
    delta: Delta option
    index: int option }

let deserializeActive (json: string) =
  try
    System.Text.Json.JsonSerializer.Deserialize<Message> json |> Some
  with _ ->
    None


let eventToMsg (line: EventLine) =
  match line with
  | Data d ->
    match deserializeActive d with
    | Some { ``type`` = "message_start" } -> None
    | Some { ``type`` = "ping" } -> None
    | Some { ``type`` = "content_block_delta"
             delta = Some { text = Some t } } -> Some(Word t)
    | _ -> None
  | _ -> None


let ask (key: Key) (m: GetProviderImpl.Model) (question: Prompt) =
  http {
    POST "https://api.anthropic.com/v1/messages"
    header "x-api-key" key
    header "anthropic-version" "2023-06-01"
    body

    jsonSerialize
      {| model = m
         max_tokens = 1024
         stream = true
         messages = [ {| role = "user"; content = question |} ] |}
  }
  |> Request.send
  |> Response.toStream
  |> readEvents
  |> procEventLines eventToMsg

let providerModule: ProviderModule =
  { provider = "Anthropic"
    keyVar = "anthropic_key"
    implementation =
      fun key ->
        { answerer = ask key
          models = [ haiku3; sonnet3; opus3; sonnet35 ]
          _default = haiku3 } }
