#r "Lib/bin/Debug/net8.0/Lib.dll"
#r "nuget: FSharp.Control.AsyncSeq, 3.2.1"
#r "nuget: FsHttp"
#r "nuget: LamgEnv, 0.0.2"

open FSharp.Control
open System
open FsHttp

open FSharp.Control
open ProviderModuleImpl.ServerSentEvents
open Stream.Types


let key = LamgEnv.getEnv "anthropic_key" |> Option.defaultValue ""

let appendNone (xs: AsyncSeq<'a option>) =
  AsyncSeq.append xs (AsyncSeq.ofSeq [ None ])


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
    | Some { ``type`` = "message_start" } -> Some None
    | Some { ``type`` = "ping" } -> Some None
    | Some { ``type`` = "content_block_delta"
             delta = Some { text = Some t } } -> Some(Some t)
    | _ -> None
  | _ -> Some None

let procEventLines (xs: AsyncSeq<EventLine>) =
  xs |> AsyncSeq.choose eventToMsg |> AsyncSeq.skipWhile _.IsNone

let ask (key: string) (question: string) =
  http {
    POST "https://api.anthropic.com/v1/messages"
    header "x-api-key" key
    header "anthropic-version" "2023-06-01"
    body

    jsonSerialize
      {| model = "claude-3-5-sonnet-20240620"
         max_tokens = 1024
         stream = true
         messages = [ {| role = "user"; content = question |} ] |}
  }
  |> Request.send
  |> Response.toStream
  |> readEvents
  |> procEventLines
  |> AsyncSeq.iter (fun s -> printf $"{s}")
  |> Async.RunSynchronously

ask key "hola"
printfn ""
