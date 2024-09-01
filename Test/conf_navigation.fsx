type Provider =
  | OpenAI
  | GitHub
  | HuggingFace
  | Anthropic
  | ImaginePro
  | Perplexity

type Model = Model of string
type Key = Key of string

type Prompt =
  | LlmPrompt of string
  | Introduction

type Request =
  | SetProvider of Provider
  | SetModel of Provider * Model
  | SetApiKey of Provider * Key
  | Completion of Prompt
  | Skip

type Configuration =
  { model: Model
    provider: Provider
    keys: Map<Provider, Key> }

type SettingKind =
  | Switch of bool
  | Input of string

type Control =
  | Checkbox of label: string * value: bool
  | Entry of label: string * value: string
  | Group of name: string * description: string

type Setting =
  { description: string
    setting: SettingKind
    request: Request }

type Group =
  { applicableTo: Provider * Model -> bool
    name: string
    description: string
    settings: Setting array }

type State =
  { conf: Configuration
    activeGroup: int option
    groups: Group array
    event: Event<Request>
    visibleControls: Control array }

let applicableToAll (_: Provider * Model) = true
let applicableToMembers (xs: (Provider * Model) array) (p: Provider * Model) = Array.contains p xs
let applicableToProvider (p: Provider) ((x, _): Provider * Model) = p = x

[<Literal>]
let dalle3 = "dall-e-3"

[<Literal>]
let gpt4oMini = "gpt-4o-mini"

let openAIModels = [ gpt4oMini; dalle3; "gpt-4o" ]
let githubModels = [ "copilot" ]
let huggingFaceModels = [ "gpt2" ]

let imagineProAiModels = [ "midjourney" ]

let anthropicModels =
  [ "claude-3-5-sonnet-20240620"
    "claude-3-haiku-20240307"
    "claude-3-opus-20240229" ]

let perplexityModels =
  [ "llama-3.1-sonar-small-128k-online"
    "llama-3.1-sonar-large-128k-online"
    "llama-3.1-sonar-huge-128k-online" ]


let createModelsGroup provider models =
  { applicableTo = applicableToProvider provider
    name = "Models"
    description = "Active provider models"
    settings =
      models
      |> List.mapi (fun i m ->
        { description = m
          setting = Switch(i = 0)
          request = SetModel(provider, Model m) })
      |> List.toArray }

let openaiGroup = createModelsGroup OpenAI openAIModels
let githubGroup = createModelsGroup GitHub githubModels
let anthropicGroup = createModelsGroup Anthropic anthropicModels
let perplexityGroup = createModelsGroup Perplexity perplexityModels
let imagineproGroup = createModelsGroup ImaginePro imagineProAiModels

let providersGroup =
  { description = "AI platform giving access to LLMs through an API"
    name = "Set provider"
    applicableTo = applicableToAll
    settings =
      [| OpenAI; GitHub |]
      |> Array.mapi (fun i x ->
        { description = x.ToString()
          setting = Switch(i = 0)
          request = SetProvider x }) }

let originalGroups =
  [| openaiGroup; githubGroup; anthropicGroup; perplexityGroup; imagineproGroup |]

let currentGroups (conf: Configuration) (xs: Group array) =
  xs |> Array.filter (fun g -> g.applicableTo (conf.provider, conf.model))

let settingToControl =
  function
  | { description = d; setting = Switch x } -> Checkbox(d, x)
  | { description = d; setting = Input x } -> Entry(d, x)

let groupToControl g = Group(g.name, g.description)

let replace (index: int) (v: 'a) (xs: 'a array) =
  xs |> Array.mapi (fun i x -> if i = index then v else x)

let activateSetting (state: State) (index: int option, input: string option) =
  match state, index with
  | { activeGroup = Some groupIndex }, Some index ->
    let g = state.groups[groupIndex]
    let s = g.settings[index]

    let setting =
      match s with
      | { setting = Switch x } -> { s with setting = Switch(not x) }
      | { setting = Input v } ->
        let newInput = (Option.defaultValue v input)

        { s with
            setting = Input newInput
            request = SetApiKey(state.conf.provider, Key newInput) }

    let newSettings = g.settings |> replace index setting
    let newGroups = state.groups |> replace groupIndex { g with settings = newSettings }

    let newState =
      { state with
          groups = newGroups
          visibleControls = newSettings |> Array.map settingToControl }

    state.event.Trigger s.request
    newState

  | { activeGroup = Some groupIndex }, None ->
    let g = state.groups[groupIndex]

    { state with
        visibleControls = g.settings |> Array.map settingToControl }
  | { activeGroup = None }, Some index ->
    let g = state.groups[index]

    { state with
        activeGroup = Some index
        visibleControls = g.settings |> Array.map settingToControl }
  | { activeGroup = None }, None ->
    let groups = currentGroups state.conf originalGroups
    let controls = groups |> Array.map groupToControl

    { state with
        groups = groups
        visibleControls = controls }
