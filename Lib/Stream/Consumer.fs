module Stream.Consumer

open Types

let consumeStream (si: StopInsert) (read: Stream) =
  let rec loop () =
    task {
      let! r = read ()

      match r with
      | Some w ->
        si.insertWord w
        return! loop ()
      | None -> si.stop ()
    }

  loop () |> Async.AwaitTask |> Async.Start
