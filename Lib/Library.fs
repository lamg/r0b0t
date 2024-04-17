module R0b0t

open System.Threading.Channels

type Key = string

type ProviderConf =
  { name: string
    envVarKey: string }

type GuiControlState = {provider: string; model:string; prompt:string; fontDescription:string} 
type Config = {providers: ProviderConf list; controls: GuiControlState}

type Provider = {name:string; key: Key; models: string list; token: Key option }

type Client = (string * Channel<string option>) -> unit
type Implementation = { provider: Provider; client: Client }

type SelectedState = {controls: GuiControlState; client: Client }

type Prompt = { name: string; template: string}
type ProviderName = string
type Prompts = Map<ProviderName, Prompt>
type GuiState = { state: SelectedState; available: Implementation list; prompts: Prompts }

// type 'a Tree = Node of ('a * 'a list) | Leaf of 'a

type Selection = { provider: string; models: string list; prompts: string list }
let guiSelectorContent (available: Implementation list) (prompts: Prompts) = []
let loadImplementations (c: Config) = []
