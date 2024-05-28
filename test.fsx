#r "Lib/bin/Debug/net8.0/Lib.dll"
#r "nuget: FSharp.Control.AsyncSeq, 3.2.1"
#r "nuget: Betalgo.OpenAI, 8.2.2"

open FSharp.Control
open System

let key = GetProviderImpl.getEnv "openai_key" |> Option.defaultValue ""

ProviderModuleImpl.OpenAI.imagine key "hola mundo"
|> AsyncSeq.toArraySynchronously
|> Seq.iteri (fun i ->
  function
  | Some img ->
    let bs = Convert.FromBase64String img
    IO.File.WriteAllBytes($"img{i}.png", bs)
  | None -> ())
