module Provider.Openai

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

let provider (key: string option) (model: string) (question: string, answer: Channel<string option>) =
  Util.provider "openai" key model (question, answer)
