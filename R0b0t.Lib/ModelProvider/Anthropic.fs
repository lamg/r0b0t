module Anthropic

open FsHttp
open ServerSentEvents

open Configuration
open Navigation
open GtkGui

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


let eventLineToMsg (line: EventLine) =
  match line with
  | Data d ->
    match deserializeActive d with
    | Some { ``type`` = "message_start" } -> None
    | Some { ``type`` = "ping" } -> None
    | Some { ``type`` = "content_block_delta"
             delta = Some { text = Some t } } -> Some(Word t)
    | _ -> None
  | _ -> None


let ask (Key key) (Model model) (prompt: string) =
  http {
    POST "https://api.anthropic.com/v1/messages"
    header "x-api-key" key
    header "anthropic-version" "2023-06-01"
    body

    jsonSerialize
      {| model = model
         max_tokens = 1024
         stream = true
         messages = [ {| role = "user"; content = prompt |} ] |}
  }
  |> Request.send
  |> Response.toStream
  |> chooseEvents eventLineToMsg
