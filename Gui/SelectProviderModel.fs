module SelectProviderModel

open System.Threading.Channels
open Controls

type ProviderLlm = { provider: string; llm: string }

type GetProviderLlm = unit -> ProviderLlm

type GetActiveImplementation = unit -> (string * Channel<string option> -> unit)

type Config =
  { providers: Map<string, Types.Provider>
    active: ProviderLlm }

let getActiveImplementation (c: Config) () =
  c.providers[c.active.provider].implementation c.active.llm

let getActiveModels (c: Config) = c.providers[c.active.provider].models

let activateFirstModel (c: Config) (provider: string) =
  { c with
      active =
        { provider = provider
          llm = c.providers[provider].models.Head } }

let confProviderSelectorUpdate (c: Config) (p: ProviderLlmSelectors) =
  c.providers.Keys |> Seq.iter (fun x -> p.providerSelector.AppendText x)
  p.providerSelector.Active <- 0

  let mutable nc = c

  let updateModels _ =
    let currentProvider = p.providerSelector.ActiveText
    nc <- activateFirstModel nc currentProvider

    p.llmSelector.RemoveAll()
    let models = getActiveModels c
    models |> List.iter p.llmSelector.AppendText
    let activeIndex = List.findIndex (fun x -> c.active.llm = x) models
    p.llmSelector.Active <- activeIndex
    p.providerLabel.Text <- c.active.provider

  p.llmSelector.Changed.Add(fun _ ->
    nc <-
      { nc with
          active.llm = p.llmSelector.ActiveText }

    p.modelLabel.Text <- nc.active.llm)

  p.providerSelector.Changed.Add updateModels
  updateModels ()

  getActiveImplementation nc
  : GetActiveImplementation
