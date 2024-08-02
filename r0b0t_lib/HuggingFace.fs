module r0b0tLib.HuggingFace

open FsHttp
open Core
open ServerSentEvents

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

let ask (Key key) (Model model) (Prompt prompt) =
  http {
    POST $"https://api-inference.huggingface.co/models/{model}"
    AuthorizationBearer key

    body
    jsonSerialize {| inputs = prompt; stream = true |}
  }
  |> Request.send
  |> Response.toStream
  |> chooseEvents eventToMsg
