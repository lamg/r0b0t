module ModelProvider.ImaginePro

open System.IO
open System.Net
open FSharp.Control
open FsHttp

open Configuration
open Navigation
open GtkGui

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

let requestImage (Key key) (prompt: string) =
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

let getImage (Key key) (prompt: string) uri =
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

let loadPng () =
  let bs = File.ReadAllBytes "example.png"

  PngData
    { prompt = "bla"
      image = bs
      revisedPrompt = "bla" }

let fakeImagine _ _ =
  Seq.unfold
    (fun acc ->
      let r = acc + 1

      if r <= 100 then Some(ProgressUpdate((float r) / 100.0), r)
      else if r = 101 then Some(loadPng (), r)
      else None)
    0
  |> AsyncSeq.ofSeq
  |> AsyncSeq.mapAsync (fun x ->
    async {
      do! Async.Sleep 100
      return x
    })

let imagine key (prompt: string) =
  let newProgress n = float n / 100.0 |> ProgressUpdate
  let messageId = requestImage key prompt |> _.messageId
  let mutable cont = true
  let mutable count = 0u
  let mutable lastProgress = 0

  asyncSeq {
    while cont do
      let! prog = checkImageProgress key messageId

      match prog with
      | { progress = Some 100; uri = Some uri } ->
        yield newProgress 99
        cont <- false
        let! img = getImage key prompt uri
        yield newProgress 100
        yield img
      | _ when count = waitForDoneLimit ->
        cont <- false
        yield Word "limit reached"
      | { progress = Some n } when n < lastProgress ->
        lastProgress <- lastProgress + 1
        yield lastProgress |> newProgress
      | { progress = None } ->
        lastProgress <- lastProgress + 1
        yield lastProgress |> newProgress

      | { progress = Some n } ->
        lastProgress <- n
        yield newProgress n

      do! Async.Sleep 5000
      count <- count + 1u
  }
