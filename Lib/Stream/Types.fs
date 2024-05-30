module Stream.Types

open FSharp.Control

type LlmData =
  | Word of string
  | PngBase64 of string

type GetProvider = unit -> AsyncSeq<LlmData option>

type StopReason =
  | Timeout
  | Done

type StopInsert =
  { insertData: LlmData -> unit
    stop: StopReason -> unit }

type Stream = unit -> Async<LlmData option option>
