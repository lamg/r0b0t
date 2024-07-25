module r0b0tLib.OpenAI

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

  let png = client.GenerateImage(p, opts)

  [ PngData
      { image = png.Value.ImageBytes.ToArray()
        prompt = p
        revisedPrompt = png.Value.RevisedPrompt } ]
  |> AsyncSeq.ofSeq

let complete (Key key) (Model m) (Prompt p) =
  let client = ChatClient(m, key, null)
  let r = client.CompleteChatStreamingAsync p

  r
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.map (fun x -> x.ContentUpdate |> Seq.head |> _.Text |> Word)
