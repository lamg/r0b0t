module r0b0tLib.ImaginePro

open System.Net
open FSharp.Control
open FsHttp

open Core

type ImagineResponse =
  { success: bool
    messageId: string
    createdAt: string }

type ProgressResponse =
  { status: string
    createdAt: string
    updatedAt: string
    messageId: string
    uri: string option
    progress: int option
    buttons: string list option
    prompt: string option }

[<Literal>]
let baseUrl = "https://api.imaginepro.ai"

let requestImage (Key key) (prompt: LlmPrompt) =
  http {
    config_useBaseUrl baseUrl
    POST "/api/v1/midjourney/imagine"
    AuthorizationBearer key

    body

    jsonSerialize {| prompt = prompt |}
  }
  |> Request.send
  |> Response.deserializeJson<ImagineResponse>

let checkImageProgress (Key key) (messageId: string) =
  async {
    let! r =
      http {
        config_useBaseUrl baseUrl
        GET $"/api/v1/midjourney/message/{messageId}"
        AuthorizationBearer key
      }
      |> Request.sendAsync

    return! r |> Response.deserializeJsonAsync<ProgressResponse>
  }

let getImage (Key key) (prompt: LlmPrompt) uri =
  async {
    let! r =
      http {
        GET uri
        AuthorizationBearer key
      }
      |> Request.sendAsync

    if r.statusCode = HttpStatusCode.OK then
      let! bs = r.ToBytesAsync() |> Async.AwaitTask

      return
        PngData
          { image = bs
            prompt = prompt
            revisedPrompt = prompt }
    else
      return Word $"Expecting OK, got {r.statusCode} {r.ToString()}"
  }

[<Literal>]
let waitForDoneLimit = 100u

let imagine key (prompt: LlmPrompt) =
  let messageId = requestImage key prompt |> _.messageId
  let mutable cont = true
  let mutable count = 0u

  asyncSeq {
    while cont do
      let! prog = checkImageProgress key messageId

      match prog with
      | { progress = Some 100; uri = Some uri } ->
        cont <- false
        let! img = getImage key prompt uri
        yield img
      | _ when count = waitForDoneLimit ->
        cont <- false
        yield Word "limit reached"
      | s ->
        yield Word $"{s.progress |> Option.defaultValue 0} "
        do! Async.Sleep 5000
        count <- count + 1u
  }
