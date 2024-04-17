module Provider.Util

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

let provideLlmAnswer (xs: string seq) (_: string) (_: string) (_: string, answer: Channel<string option>) =

  task {
    let tks = new CancellationTokenSource(TimeSpan.FromSeconds 2)

    for x in xs do
      do! Task.Delay 100
      do! answer.Writer.WriteAsync(Some $"{x} ", tks.Token).AsTask()

    do! answer.Writer.WriteAsync None
  }
  |> Async.AwaitTask
  |> Async.Start

let provider (name: string) (key: string option) (model: string) (question: string, answer: Channel<string option>) =

  match key with
  | Some k ->
    let xs = Guid.NewGuid().ToString().Split("-")
    let ys = name :: (List.ofArray xs)
    provideLlmAnswer ys k model (question, answer)

  | None ->
    let xs = [ "no"; "valid"; "API"; "key"; "for"; name ]
    provideLlmAnswer xs "" model (question, answer)
