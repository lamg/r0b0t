module ProviderModuleImpl.HuggingFace

open FSharp.Control
open LangChain.Providers
open LangChain.Providers.HuggingFace
open LangChain.Providers.HuggingFace.Predefined
open Stream.Types
open GetProviderImpl

let gpt2 = Model "gpt2"
let metaLlama3_8B = Model "meta-llama/Meta-Llama-3-8B"
let microsoftPhi2 = "microsoft/phi-2"
let googleBertUncased = "google-bert/bert-base-uncased"

let ask (key: Key) (m: GetProviderImpl.Model) (question: Prompt) =
  let client = new System.Net.Http.HttpClient()

  let genAsync =
    match m with
    | _ when m = gpt2 ->
      let prov = HuggingFaceProvider(key, client)
      Gpt2Model prov |> _.GenerateAsync
    | _ when m = metaLlama3_8B ->
      let conf = HuggingFaceConfiguration()
      conf.ApiKey <- key
      conf.ModelId <- metaLlama3_8B
      let prov = HuggingFaceProvider(conf, client)
      HuggingFaceChatModel(prov, conf.ModelId).GenerateAsync
    | _ -> failwith $"unsupported model {m}"

  let msg = genAsync (ChatRequest.op_Implicit question)

  [ msg.Result.LastMessageContent |> Word |> Some; None ] |> AsyncSeq.ofSeq


let providerModule: ProviderModule =
  { provider = "HuggingFace"
    keyVar = "huggingface_key"
    implementation =
      fun key ->
        { answerer = ask key
          models = [ gpt2; metaLlama3_8B; microsoftPhi2; googleBertUncased ]
          _default = microsoftPhi2 } }
