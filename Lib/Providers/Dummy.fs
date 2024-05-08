module R0b0t.Provider.Dummy

open System
open Types
open FSharp.Control

let ask (_: Model) (_: Question) =
  let xs = Guid.NewGuid().ToString().Split "-"

  let ays =
    AsyncSeq.unfoldAsync
      (fun i ->
        async {
          do! Async.Sleep 400

          return if i < xs.Length then Some($"{xs[i]} ", i + 1) else None
        })
      0

  MailboxProcessor.Start(fun inbox -> Util.readSegments inbox ays)

[<Literal>]
let providerName = "Dummy"

[<Literal>]
let defaultModel = "Dummy"

let getProvider (_: Key) =
  { name = providerName
    models = [ defaultModel ]
    modelAnswerer = ask }
