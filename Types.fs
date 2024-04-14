module Types

open System.Threading.Channels

type Provider =
  { name: string
    envVar: string
    key: string option
    models: string list }

type Implementation = string -> string -> (string * Channel<string option>) -> unit

type Config =
  { provider: Map<string, Provider>
    implementation: Implementation }
