module Stream.Types

open FSharp.Control

type GetProvider = unit -> AsyncSeq<string>

type StopInsert =
  { insertWord: string -> unit
    stop: unit -> unit }

type Stream = unit -> Async<string option>
