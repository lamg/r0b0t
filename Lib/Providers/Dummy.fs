module R0b0t.Provider.Dummy

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Channels

let provider (_: string) (_: string) (_: string, answer: Channel<string option>) =
  let xs = Guid.NewGuid().ToString().Split("-")
  let ys = "dummy" :: (List.ofArray xs)
  Util.sendAnswer ys answer