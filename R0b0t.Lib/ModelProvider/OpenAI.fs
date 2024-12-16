module ModelProvider.OpenAI

open OpenAI.Chat
open OpenAI.Images
open FSharp.Control

open Configuration
open Navigation
open GtkGui

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

let imagine (Key key) (prompt: string) =
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
      let! png = client.GenerateImageAsync(prompt, opts) |> Async.AwaitTask
      yield ProgressUpdate 1.0

      yield
        PngData
          { image = png.Value.ImageBytes.ToArray()
            prompt = prompt
            revisedPrompt = png.Value.RevisedPrompt }
    }

  AsyncSeq.merge (progressSeq ()) imageSeq
  |> AsyncSeq.takeWhileInclusive (function
    | Word _
    | PngData _ -> false
    | _ -> true)

let complete (Key key) (Model m) (prompt: string) =
  let client = ChatClient(m, (System.ClientModel.ApiKeyCredential key), null)
  let r = client.CompleteChatStreamingAsync prompt

  r
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.choose (fun x -> x.ContentUpdate |> Seq.tryHead |> Option.map (_.Text >> Word))
