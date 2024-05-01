module R0b0t.Provider.Dummy

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

open Types

let provider (answer: Channel<string option>) =
  let xs = Guid.NewGuid().ToString().Split("-")
  let ys = "dummy" :: (List.ofArray xs)
  Util.sendAnswer ys answer

let getProvider () =
  { name = "OpenAI"
    models = [ "Dummy" ]
    implementation = (fun _ (_, answer) -> provider answer) }

let provider2 (inbox: MailboxProcessor<Message>) =
  let rec loop (xs: string list) =
    async {
      // Receive a message
      let! msg = inbox.Receive()
      do! Async.Sleep 500

      return!
        match msg, xs with
        | Question q, _ -> loop xs
        | AnswerSegment chan, y :: ys ->
          chan.Reply $" {y}"
          loop ys
        | Stop chan, _
        | AnswerSegment chan, _ ->
          chan.Reply "\n\n"
          async { () }
    }

  let xs = Guid.NewGuid().ToString().Split("-")
  let ys = "dummy" :: (List.ofArray xs)
  loop ys
