module Stream.Consumer

open Types

let consumeStream (si: StopInsert) (read: Stream) =
  let rec loop () =
    async {
      let! r = read ()

      match r with
      | Some(Some w) ->
        si.insertWord w
        return! loop ()
      | Some None ->
        // sequence fully consumed
        si.stop ()
      | None ->
        // timeout
        si.stop ()
    }

  loop () |> Async.Start
