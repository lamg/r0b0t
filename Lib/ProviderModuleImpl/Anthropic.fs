module ProviderModuleImpl.Anthropic

open Anthropic.SDK.Messaging
open FSharp.Control
open GetProviderImpl
open LangChain.Providers
open LangChain.Providers.Anthropic
open LangChain.Providers.Anthropic.Predefined
open Stream.Types

let appendNone (xs: AsyncSeq<'a option>) =
  AsyncSeq.append xs (AsyncSeq.ofSeq [ None ])

let ask (key: Key) (m: GetProviderImpl.Model) (question: Prompt) =
  let conf = AnthropicConfiguration(ApiKey = key)

  let client = AnthropicProvider.FromConfiguration conf
  let prov = Claude3Haiku client

  [ prov.GenerateAsync(question).Result.LastMessageContent |> Word |> Some; None ]
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

let haiku =
  GetProviderImpl.Model Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku

let providerModule: ProviderModule =
  { provider = "Anthropic"
    keyVar = "anthropic_key"
    implementation =
      fun key ->
        { answerer = ask key
          models = [ haiku ]
          _default = haiku } }
