module Stream.Consumer

open Types

let consumeStream (si: StopInsert) (read: Stream) =
  let rec loop () =
    async {
      let! r = read ()

      match r with
      | Some(Some w) ->
        si.insertData w
        return! loop ()
      | Some None ->
        // sequence fully consumed
        si.stop Done
      | None ->
        // timeout
        si.stop Timeout
    }

  loop () |> Async.Start
