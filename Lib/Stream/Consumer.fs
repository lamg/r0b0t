module Stream.Consumer

open System

open Types

let consumeStream (si: StopInsert) (read: Stream) =
  let rec loop () =
    async {
      let! r = read ()

      match r with
      | Some(Some(PngData { image = [||] })) -> return! loop ()
      | Some(Some(Word w)) when String.IsNullOrEmpty w -> return! loop ()
      | Some(Some w) ->
        si.insertData w
        return! loop ()
      | Some None ->
        // sequence fully consumed
        si.insertData (Word "\n\n")
        si.stop Done
      | None ->
        // timeout
        si.insertData (Word "timeout\n\n")
        si.stop Timeout
    }

  loop () |> Async.Start
