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
