module ReadAnswer

open System.Threading.Channels
open Controls


type AddText = string option -> unit

type ReadAnswer = Channel<string option> -> unit

type StopInsert = {stop: unit -> unit; insertWord: string -> unit }
type StartAddText = {start: unit -> unit; addText: AddText }

let newAddText (stopInsert: StopInsert) (word: string option) =
  match word with
  | Some w -> stopInsert.insertWord w

  | None ->
    stopInsert.insertWord "\n\n"
    stopInsert.stop ()

let readAnswer (sa: StartAddText) (answer: Channel<string option>) =
  let rec loop () =
    task {
      let! r = answer.Reader.ReadAsync()

      (fun _ ->
        sa.addText r
        false)
      |> GLib.Idle.Add
      |> ignore

      if r.IsSome then
        return! loop ()
    }
  sa.start()
  loop () |> Async.AwaitTask |> Async.Start
