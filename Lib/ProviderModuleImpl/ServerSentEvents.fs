module ProviderModuleImpl.ServerSentEvents

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

let readEvents (sr: IO.Stream) =
  let rd = new IO.StreamReader(sr)

  asyncSeq {
    while not rd.EndOfStream do
      let! line = rd.ReadLineAsync() |> Async.AwaitTask

      match parseEventLine line with
      | Some e -> yield e
      | None -> ()
  }
