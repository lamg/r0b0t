module R0b0t.Provider.Openai

open System.Threading.Channels

open OpenAI
open OpenAI.Managers
open OpenAI.ObjectModels
open OpenAI.ObjectModels.RequestModels

open FSharp.Control

open Types

let writeChan (chan: 'a Channel) (x: 'a) =
  x |> chan.Writer.WriteAsync |> _.AsTask() |> Async.AwaitTask

let streamAnswer (client: OpenAIService) (model: string) (chan: Channel<string option>) (xs: ChatMessage list) =
  let resp =
    client.CreateCompletionAsStream(ChatCompletionCreateRequest(Model = model, Messages = (xs |> ResizeArray)))

  // FIXME when the key is not valid just an empty string is sent, and nothing else happens
  resp
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.foldAsync
    (fun acc x ->
      async {

        if x.Successful then
          let segment = x.Choices |> Seq.head |> (fun y -> y.Message.Content)
          do! writeChan chan (Some segment)
          return $"{acc}{segment}"
        else
          do! writeChan chan (Some x.Error.Message)
          return acc
      })
    ""

let answerWithData (key: string) (model: string) (context: string) (question: string, chan: Channel<string option>) =
  let client = new OpenAIService(OpenAiOptions(ApiKey = key))

  let messages =
    [ ChatMessage.FromSystem(
        $"Answer the question based on the context below, and if the question can't be answered based on the context, say \"I don't know\".
          Context: {context}"
      ) ]


  let messages = messages @ [ ChatMessage.FromUser question ]
  streamAnswer client model chan messages

let ask (key: Key) (model: Model) (question: string, chan: Channel<string option>) =
  match question with
  | "" -> chan.Writer.WriteAsync None |> _.AsTask() |> Async.AwaitTask |> Async.Start
  | _ ->
    async {
      let! _ = answerWithData key model "You are an useful AI" (question, chan)
      do! writeChan chan None
      return ()
    }
    |> Async.Start

[<Literal>]
let environmentVar = "openai_key"

[<Literal>]
let providerName = "OpenAI"


let defaultModel = Models.Gpt_4_turbo

let getProvider (key: string) =
  { name = providerName
    models =
      [ Models.Gpt_3_5_Turbo
        Models.Gpt_4
        Models.Gpt_3_5_Turbo_16k
        Models.Gpt_4_turbo ]
    implementation = ask key }
