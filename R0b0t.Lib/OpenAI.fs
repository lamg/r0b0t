module OpenAI

open FSharp.Control
open OpenAI.Chat
open OpenAI.Images

open Core

let imagine (Key key) (Prompt p) =
  let client = ImageClient(dalle3, key)

  let opts =
    ImageGenerationOptions(
      Quality = GeneratedImageQuality.High,
      Size = GeneratedImageSize.W1792xH1024,
      Style = GeneratedImageStyle.Vivid,
      ResponseFormat = GeneratedImageFormat.Bytes
    )

  [ async {
      let! png = client.GenerateImageAsync(p, opts) |> Async.AwaitTask

      return
        PngData
          { image = png.Value.ImageBytes.ToArray()
            prompt = p
            revisedPrompt = png.Value.RevisedPrompt }
    } ]
  |> AsyncSeq.ofSeqAsync


let complete (Key key) (Model m) (Prompt p) =
  let client = ChatClient(m, key, null)
  let r = client.CompleteChatStreamingAsync p

  r
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.map (fun x -> x.ContentUpdate |> Seq.head |> _.Text |> Word)
