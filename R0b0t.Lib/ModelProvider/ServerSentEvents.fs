module ServerSentEvents

open System.Threading
open System.Threading.Tasks
open FSharp.Control
open System

type EventLine =
  | Event of string
  | Data of string
  | Id of int
  | Retry of int

let trimPrefix (prefix: string) (line: string) =
  match line.Split(prefix, StringSplitOptions.TrimEntries) with
  | [| _; x |] -> Some x
  | _ -> None

let parseInt (s: string) =
  let mutable n = 0
  if Int32.TryParse(s, ref n) then Some n else None

let parseEvent = trimPrefix "event:" >> Option.map Event
let parseData = trimPrefix "data:" >> Option.map Data
let parseId = trimPrefix "id:" >> Option.bind parseInt >> Option.map Id
let parseRetry = trimPrefix "retry:" >> Option.bind parseInt >> Option.map Retry

let parseEventLine (line: string) =
  [ parseEvent; parseData; parseId; parseRetry ]
  |> Seq.map (fun x -> x line)
  |> Seq.tryFind _.IsSome
  |> Option.defaultValue None

let chooseEvents (f: EventLine -> 'a option) (stream: IO.Stream) =
  let rd = new IO.StreamReader(stream)
  let tks = new CancellationTokenSource()
  tks.CancelAfter 10_000
  let mutable cont = true

  asyncSeq {
    while cont do
      try
        let! line = rd.ReadLineAsync(tks.Token).AsTask() |> Async.AwaitTask

        if line <> null then
          match parseEventLine line with
          | Some e ->
            match f e with
            | Some x -> yield x
            | None -> ()
          | None -> ()
        else
          cont <- false
      with
      | :? ArgumentNullException
      | :? NullReferenceException
      | :? TaskCanceledException -> cont <- false
      | e ->
        cont <- false
        eprintfn $"unexpected exception chooseEvents {e.Message}"
  }
