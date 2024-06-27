module ProviderModuleImpl.HuggingFace

open FSharp.Control
open FsHttp
open Stream.Types
open GetProviderImpl
open ServerSentEvents

let gpt2 = Model "gpt2"
let metaLlama3_8B = Model "meta-llama/Meta-Llama-3-8B"
let microsoftPhi2 = "microsoft/phi-2"
let googleBertUncased = "google-bert/bert-base-uncased"

type Message =
  { index: int
    token: {| text: string |} }

let deserializeActive (json: string) =
  try
    System.Text.Json.JsonSerializer.Deserialize<Message> json |> Some
  with _ ->
    None


let eventToMsg (line: EventLine) =
  match line with
  | Data d ->
    match deserializeActive d with
    | Some { token = t } -> Some(Word t.text)
    | _ -> None
  | _ -> None

let ask (key: Key) (m: GetProviderImpl.Model) (question: Prompt) =
  http {
    POST "https://api-inference.huggingface.co/models/microsoft/phi-2"
    AuthorizationBearer key

    body

    jsonSerialize {| inputs = question; stream = true |}
  }
  |> Request.send
  |> Response.toStream
  |> readEvents
  |> procEventLines eventToMsg

let providerModule: ProviderModule =
  { provider = "HuggingFace"
    keyVar = "huggingface_key"
    implementation =
      fun key ->
        { answerer = ask key
          models = [ gpt2; metaLlama3_8B; microsoftPhi2; googleBertUncased ]
          _default = microsoftPhi2 } }
