module Stream.Producer

open Types
open FSharp.Control

type Message = AnswerSegment of AsyncReplyChannel<string>
type Provider = MailboxProcessor<Message> -> Async<unit>

let readSegments (inbox: MailboxProcessor<Message>) (xs: AsyncSeq<string>) =
  xs
  |> AsyncSeq.takeWhileAsync (fun x ->
    async {
      let! msg = inbox.TryReceive()

      return
        match msg with
        | Some(AnswerSegment chan) ->
          chan.Reply x
          true
        | _ -> false
    })
  |> AsyncSeq.toListAsync
  |> Async.Ignore

let produceStream (g: GetProvider) =
  let mb = MailboxProcessor.Start(fun inbox -> g () |> readSegments inbox)
  fun () -> mb.PostAndTryAsyncReply(AnswerSegment, timeout = 1000)
