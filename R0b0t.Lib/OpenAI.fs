module OpenAI

open FSharp.Control
open OpenAI.Chat
open OpenAI.Images

open Core

let totalSeconds = 90

let progressSeq () =
  AsyncSeq.unfoldAsync
    (fun state ->
      async {
        do! Async.Sleep 1000 // wait a second
        let newState = state + 1

        return
          if newState < totalSeconds then
            Some(ProgressUpdate((float newState) / 100.0), newState)
          else if newState = totalSeconds then
            Some(Word "timeout waiting for image", newState)
          else
            None
      })
    0

let imagine (Key key) (p: LlmPrompt) =
  let client = ImageClient(dalle3, key)

  let opts =
    ImageGenerationOptions(
      Quality = GeneratedImageQuality.High,
      Size = GeneratedImageSize.W1792xH1024,
      Style = GeneratedImageStyle.Vivid,
      ResponseFormat = GeneratedImageFormat.Bytes
    )

  let imageSeq =
    asyncSeq {
      let! png = client.GenerateImageAsync(p, opts) |> Async.AwaitTask
      yield ProgressUpdate 1.0

      yield
        PngData
          { image = png.Value.ImageBytes.ToArray()
            prompt = p
            revisedPrompt = png.Value.RevisedPrompt }
    }

  AsyncSeq.merge (progressSeq ()) imageSeq
  |> AsyncSeq.takeWhileInclusive (function
    | Word _
    | PngData _ -> false
    | _ -> true)

let complete (Key key) (Model m) (p: LlmPrompt) =
  let client = ChatClient(m, key, null)
  let r = client.CompleteChatStreamingAsync p

  r
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.map (fun x -> x.ContentUpdate |> Seq.head |> _.Text |> Word)
