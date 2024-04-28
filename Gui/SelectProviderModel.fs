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
  p.providerLabel.Text <- c.active.provider
  p.modelLabel.Text <- c.active.llm

  getActiveImplementation c
  : GetActiveImplementation
