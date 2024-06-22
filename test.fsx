#r "Lib/bin/Debug/net8.0/Lib.dll"
#r "nuget: FSharp.Control.AsyncSeq, 3.2.1"
#r "nuget: Betalgo.OpenAI, 8.2.2"
#r "nuget: LangChain.Providers.Anthropic"
#r "nuget: LangChain"
#r "nuget: LamgEnv, 0.0.2"

open FSharp.Control
open System

open Anthropic.SDK.Messaging
open FSharp.Control
open GetProviderImpl
open LangChain.Providers
open LangChain.Providers.Anthropic
open Stream.Types


let key = LamgEnv.getEnv "anthropic_key" |> Option.defaultValue ""

let appendNone (xs: AsyncSeq<'a option>) =
  AsyncSeq.append xs (AsyncSeq.ofSeq [ None ])

let ask (key: string) (question: string) =
  let conf = AnthropicConfiguration(ApiKey = key)
  conf.Streaming <- true
  conf.ModelId <- Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku

  let client = AnthropicProvider.FromConfiguration conf
  let msg = MessageParameters()
  msg.Stream <- true
  msg.MaxTokens <- 500
  msg.Model <- Anthropic.SDK.Constants.AnthropicModels.Claude3Haiku
  msg.Messages <- System.Collections.Generic.List()
  Anthropic.SDK.Messaging.Message(RoleType.User, question) |> msg.Messages.Add

  // let h = LangChain.Providers.Anthropic.Predefined.Claude3Haiku client
  // h.GenerateAsync("bla").Result.Messages.
  client.Api.Messages.StreamClaudeMessageAsync(msg)
  |> AsyncSeq.ofAsyncEnum
  |> AsyncSeq.iter (fun x -> printfn $"{x.StreamStartMessage}")


ask key "hola" |> Async.RunSynchronously
