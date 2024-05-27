module Stream.Main

open Types
open Consumer
open Producer

let main (g: GetProvider) (si: StopInsert) = produceStream g |> consumeStream si
