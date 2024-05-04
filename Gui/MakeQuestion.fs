module MakeQuestion

open FSharp.Control

open Types

type QuestionAnswerer =
  { getQuestion: unit -> Question
    answerer: Answerer }

type ReadStop =
  { stop: unit -> Async<string option>
    read: unit -> Async<string option> }

let makeQuestion (qa: QuestionAnswerer) =
  let mb = qa.getQuestion () |> qa.answerer

  let pr x () =
    mb.PostAndTryAsyncReply(x, timeout = 10000)

  { stop = pr Stop
    read = pr AnswerSegment }
