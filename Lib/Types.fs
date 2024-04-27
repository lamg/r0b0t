module Types

open System.Threading.Channels

type Key = string
type Model = string

type Provider =
  { name: string
    models: string list
    implementation: Model -> (string * Channel<string option>) -> unit }
