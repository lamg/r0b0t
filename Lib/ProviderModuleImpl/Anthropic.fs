module ProviderModuleImpl.Anthropic

open FSharp.Control
open GetProviderImpl
open LangChain.Providers
open LangChain.Providers.Anthropic
open LangChain.Providers.Anthropic.Predefined
open Stream.Types

let appendNone (xs: AsyncSeq<'a option>) =
  AsyncSeq.append xs (AsyncSeq.ofSeq [ None ])

let haiku3 =
  GetProviderImpl.Model Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku

let sonnet3 =
  GetProviderImpl.Model Anthropic.SDK.Constants.AnthropicModels.Claude3Sonnet

let opus3 =
  GetProviderImpl.Model Anthropic.SDK.Constants.AnthropicModels.Claude3Opus

let ask (key: Key) (m: GetProviderImpl.Model) (question: Prompt) =
  let conf = AnthropicConfiguration(ApiKey = key)

  let client = AnthropicProvider.FromConfiguration conf

  let prov =
    match m with
    | _ when m = haiku3 -> Claude3Haiku client |> _.GenerateAsync
    | _ when m = sonnet3 -> Claude3Sonnet client |> _.GenerateAsync
    | _ when m = opus3 -> Claude3Opus client |> _.GenerateAsync
    | _ -> failwith $"unknown model {m}"

  [ prov(ChatRequest.op_Implicit question).Result.LastMessageContent |> Word |> Some
    None ]
  |> AsyncSeq.ofSeq

// let msg = MessageParameters()
// msg.Stream<-false
// Anthropic.SDK.Messaging.Message(RoleType.User, question)
// |> msg.Messages.Add
//
// let r = client.Api.Messages.GetClaudeMessageAsync(msg)
// [r.Result.FirstMessage.Text |> Word |> Some; None]
// |> AsyncSeq.ofSeq
// let h = LangChain.Providers.Anthropic.Predefined.Claude3Haiku client
// h.GenerateAsync("bla").Result.Messages.
// client.Api.Messages.StreamClaudeMessageAsync(msg)
// |> AsyncSeq.ofAsyncEnum
// |> AsyncSeq.map (_.FirstMessage.Text >> Word >> Some)
// |> appendNone
//

let providerModule: ProviderModule =
  { provider = "Anthropic"
    keyVar = "anthropic_key"
    implementation =
      fun key ->
        { answerer = ask key
          models = [ haiku3; sonnet3; opus3 ]
          _default = haiku3 } }
