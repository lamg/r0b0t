module Types

open FSharp.Control

type Key = string
type Model = string
type Provider = string
type Prompt = string

type Message =
  | AnswerSegment of AsyncReplyChannel<string>
  | Stop of AsyncReplyChannel<string>

type Answerer = Prompt -> MailboxProcessor<Message>

type ProviderAnswerers =
  { name: string
    models: Model list
    modelAnswerer: Model -> Answerer }

type ProviderModel = { provider: Provider; model: Model }

type Config =
  { providers: Map<string, ProviderAnswerers>
    active: ProviderModel }
