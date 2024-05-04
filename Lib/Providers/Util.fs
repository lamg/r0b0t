module R0b0t.Provider.Util

open FSharp.Control
open Types

let readSegments (inbox: MailboxProcessor<Message>) (xs: AsyncSeq<string>) =
  xs
  |> AsyncSeq.takeWhileAsync (fun x ->
    async {
      let! msg = inbox.Receive()

      return
        match msg with
        | AnswerSegment chan ->
          chan.Reply x
          true
        | _ -> false
    })
  |> AsyncSeq.toListAsync
  |> Async.Ignore
