module Stream.Types

open FSharp.Control

type PngData =
  { image: byte array
    prompt: string
    revisedPrompt: string }

type LlmData =
  | Word of string
  | PngData of PngData

type GetProvider = unit -> AsyncSeq<LlmData option>

type StopReason =
  | Timeout
  | Done

type StopInsert =
  { insertData: LlmData -> unit
    stop: StopReason -> unit }

type Stream = unit -> Async<LlmData option option>
