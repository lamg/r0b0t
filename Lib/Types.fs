module Types

open System.Threading.Channels

type Key = string
type Model = string

type Provider =
  { name: string
    envVar: string
    key: Key option
    models: string list }

type Implementation = Key -> Model -> (string * Channel<string option>) -> unit

type Config =
  { provider: Map<string, Provider>
    implementation: Implementation }
