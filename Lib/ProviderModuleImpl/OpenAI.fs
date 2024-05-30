module ProviderModuleImpl.OpenAI

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels
open OpenAI.ObjectModels.ResponseModels

open FSharp.Control

open GetProviderImpl
open Stream.Types

let toAsyncSeqString resp =
  resp
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.map (fun (x: ChatCompletionCreateResponse) ->

    if x.Successful then
      x.Choices |> Seq.head |> _.Message.Content |> Word |> Some
    else
      None)
  |> fun xs ->
      let last = [ None ] |> AsyncSeq.ofSeq
      AsyncSeq.append xs last

let imagineFake (key: Key) (description: Prompt) =
  let bs =
    System.IO.File.ReadAllBytes "logo_small.png" |> System.Convert.ToBase64String

  [ Some(PngBase64 bs); None ] |> AsyncSeq.ofSeq

let imagine (key: Key) (description: Prompt) =
  let client = new OpenAIService(OpenAiOptions(ApiKey = key))

  let req =
    ImageCreateRequest(
      Prompt = description,
      N = 1,
      Size = StaticValues.ImageStatics.Size.Size1024,
      ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Base64,
      User = "r0b0t"
    )

  req.Model <- Models.Dall_e_3
  req.Quality <- StaticValues.ImageStatics.Quality.Hd

  async {
    let! resp = client.Image.CreateImage req |> Async.AwaitTask

    let imgs =
      if resp.Successful then
        resp.Results |> Seq.map (_.B64 >> PngBase64 >> Some)
      else
        resp.Error.Messages |> Seq.map (Word >> Some)

    return Seq.append imgs [ None ]
  }
  |> Async.RunSynchronously
  |> AsyncSeq.ofSeq

let stream (key: Key) (model: Model) (question: string) =
  let client = new OpenAIService(OpenAiOptions(ApiKey = key))

  let messages =
    [ ChatMessage.FromSystem "You are a helpful AI assistant"
      ChatMessage.FromUser question ]
    |> ResizeArray

  client.CreateCompletionAsStream(ChatCompletionCreateRequest(Model = model, Messages = messages))

let text (key: Key) (model: Model) (question: Prompt) =
  stream key model question |> toAsyncSeqString

let ask (key: Key) (model: Model) (question: Prompt) =
  match model with
  | m when m = Models.Dall_e_3 -> imagine key question
  | _ -> text key model question

let providerModule: ProviderModule =
  { provider = "OpenAI"
    keyVar = "openai_key"
    implementation =
      fun key ->
        { answerer = ask key
          models =
            [ Models.Gpt_4o
              Models.Gpt_3_5_Turbo
              Models.Gpt_4
              Models.Gpt_3_5_Turbo_16k
              Models.Gpt_4_turbo
              Models.Dall_e_3 ]
          _default = Models.Gpt_4o } }
