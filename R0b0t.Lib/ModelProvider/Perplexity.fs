module Perplexity

open FsHttp
open ServerSentEvents

open Configuration
open Navigation
open GtkGui


type Usage =
  { prompt_tokens: int
    completion_tokens: int
    total_tokens: int }

type PerplexityMessage = { role: string; content: string }
type Delta = { role: string; content: string }

type PerplexityChoice =
  { index: int
    finish_reason: string option
    message: PerplexityMessage
    delta: Delta }

type Message =
  { id: string
    model: obj option
    created: uint64
    usage: Usage
    object: string
    choices: PerplexityChoice list }

let deserializeActive (json: string) =
  try
    System.Text.Json.JsonSerializer.Deserialize<Message> json |> Some
  with _ ->
    None


let eventLineToMsg (line: EventLine) =
  match line with
  | Data d ->
    match deserializeActive d with
    | Some { choices = x :: _ } -> Some(Word x.delta.content)
    | _ -> None
  | _ -> None

let ask (Key key) (Model model) (prompt: string) =
  http {
    POST "https://api.perplexity.ai/chat/completions"
    AuthorizationBearer key
    body

    jsonSerialize
      {| model = model
         max_tokens = 9000
         stream = true
         messages = [ {| role = "user"; content = prompt |} ] |}
  }
  |> Request.send
  |> Response.toStream
  |> chooseEvents eventLineToMsg
