module Provider.Github

open System.Threading.Channels

let provider (key: string option) (model: string) (question: string, answer: Channel<string option>) =
  Util.provider "github" key model (question, answer)
