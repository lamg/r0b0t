module R0b0t.Provider.Openai

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels
open OpenAI.ObjectModels.ResponseModels

open FSharp.Control

open Types

let toAsyncSeqString resp =
  resp
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.choose (fun (x: ChatCompletionCreateResponse) ->
    if x.Successful then
      x.Choices |> Seq.head |> _.Message.Content |> Some
    else
      None)

let stream (key: Key) (model: Model) (question: string) =
  let client = new OpenAIService(OpenAiOptions(ApiKey = key))

  let messages =
    [ ChatMessage.FromSystem $"You are a helpful AI assistant"
      ChatMessage.FromUser question ]
    |> ResizeArray

  client.CreateCompletionAsStream(ChatCompletionCreateRequest(Model = model, Messages = messages))

let ask (key: Key) (model: Model) (question: Question) =
  MailboxProcessor.Start(fun inbox -> stream key model question |> toAsyncSeqString |> Util.readSegments inbox)

[<Literal>]
let environmentVar = "openai_key"

[<Literal>]
let providerName = "OpenAI"

let defaultModel = Models.Gpt_4o

let getProvider (key: Key) =
  { name = providerName
    models =
      [ Models.Gpt_3_5_Turbo
        Models.Gpt_4
        Models.Gpt_3_5_Turbo_16k
        Models.Gpt_4_turbo
        Models.Gpt_4o ]
    modelAnswerer = ask key }
