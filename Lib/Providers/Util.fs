module R0b0t.Provider.Util

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

let sendAnswer (xs: string seq) (answer: Channel<string option>) =
  task {
    let tks = new CancellationTokenSource(TimeSpan.FromSeconds 2)

    for x in xs do
      do! Task.Delay 100
      do! answer.Writer.WriteAsync(Some $"{x} ", tks.Token).AsTask()

    do! answer.Writer.WriteAsync None
  }
  |> Async.AwaitTask
  |> Async.Start
