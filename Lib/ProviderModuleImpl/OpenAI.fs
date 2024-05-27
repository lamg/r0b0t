module ProviderModuleImpl.OpenAI

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels
open OpenAI.ObjectModels.ResponseModels

open FSharp.Control

open GetProviderImpl

let toAsyncSeqString resp =
  resp
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.map (fun (x: ChatCompletionCreateResponse) ->

    if x.Successful then
      x.Choices |> Seq.head |> _.Message.Content |> Some
    else
      None)
  |> fun xs ->
      let last = [ None ] |> AsyncSeq.ofSeq
      AsyncSeq.append xs last

let stream (key: Key) (model: Model) (question: string) =
  let client = new OpenAIService(OpenAiOptions(ApiKey = key))

  let messages =
    [ ChatMessage.FromSystem $"You are a helpful AI assistant"
      ChatMessage.FromUser question ]
    |> ResizeArray

  client.CreateCompletionAsStream(ChatCompletionCreateRequest(Model = model, Messages = messages))

let ask (key: Key) (model: Model) (question: Prompt) =
  stream key model question |> toAsyncSeqString

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
              Models.Gpt_4_turbo ] } }
