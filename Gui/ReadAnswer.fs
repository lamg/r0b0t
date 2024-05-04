module ReadAnswer

open Controls
open FSharp.Control

let readAnswer (sa: StartAddText) (read: unit -> Async<string option>) =
  let rec loop () =
    task {
      let! r = read ()

      (fun _ ->
        sa.addText r
        false)
      |> GLib.Idle.Add
      |> ignore

      if r.IsSome then
        return! loop ()
    }

  sa.start ()
  loop () |> Async.AwaitTask |> Async.Start
