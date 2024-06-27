#r "Lib/bin/Debug/net8.0/Lib.dll"
#r "nuget: FSharp.Control.AsyncSeq, 3.2.1"
#r "nuget: FsHttp"
#r "nuget: LamgEnv, 0.0.2"
#r "nuget: dotenv.net"

open FSharp.Control
open System
open FsHttp

open FSharp.Control
open ProviderModuleImpl.ServerSentEvents
open Stream.Types


dotenv.net.DotEnv.Load()
let key = LamgEnv.getEnv "huggingface_key" |> _.Value

let appendNone (xs: AsyncSeq<'a option>) =
  AsyncSeq.append xs (AsyncSeq.ofSeq [ None ])

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
    | Some { token = t } -> Some(Some t.text)
    | _ -> None
  | _ -> Some None

let procEventLines (xs: AsyncSeq<EventLine>) =
  xs |> AsyncSeq.choose eventToMsg |> AsyncSeq.skipWhile _.IsNone

let ask (key: string) (question: string) =

  http {
    POST "https://api-inference.huggingface.co/models/microsoft/phi-2"
    AuthorizationBearer key

    body

    jsonSerialize {| inputs = question; stream = true |}
  }
  |> Request.send
  // |> Response.print
  // |> printfn "%s"
  |> Response.toStream
  |> readEvents
  |> procEventLines
  |> AsyncSeq.iter (function
    | Some s -> printf $"{s}"
    | _ -> ())
  |> Async.RunSynchronously

ask key "Cómo te llamas"
printfn ""
