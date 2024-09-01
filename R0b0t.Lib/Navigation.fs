module Navigation

open System
open Configuration

type Prompt =
  | LlmPrompt of string
  | Introduction

type Request =
  | SetProvider of Provider
  | SetModel of Provider * Model
  | SetApiKey of Provider * Key
  | Completion of Prompt
  | Skip

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
      [| Anthropic; OpenAI; Perplexity; GitHub; HuggingFace; ImaginePro |]
      |> Array.mapi (fun i x ->
        { description = x.ToString()
          setting = Switch(i = 0)
          request = SetProvider x }) }

let originalGroups =
  [| providersGroup
     openaiGroup
     githubGroup
     anthropicGroup
     perplexityGroup
     imagineproGroup |]

let currentGroups (conf: Configuration) (xs: Group array) =
  xs
  |> Array.choose (fun g ->

    if g.applicableTo (conf.provider, conf.model) then
      let settings =
        g.settings
        |> Array.map (function
          | { request = SetModel(p, m) } as s ->
            { s with
                setting = Switch(p = conf.provider && m = conf.model) }
          | { request = SetProvider p } as s ->
            { s with
                setting = Switch(p = conf.provider) }
          | s -> s)

      Some { g with settings = settings }
    else
      None)

let settingToControl =
  function
  | { description = d; setting = Switch x } -> Checkbox(d, x)
  | { description = d; setting = Input x } -> Entry(d, x)

let groupToControl g = Group(g.name, g.description)

let replace (index: int) (v: 'a) (xs: 'a array) =
  xs |> Array.mapi (fun i x -> if i = index then v else x)

let activateSetting (state: State, mng: ConfigurationManager) (index: int option, input: string option) =
  match state, index with
  | { activeGroup = Some groupIndex }, Some index ->
    let g = state.groups[groupIndex]
    let s = g.settings[index]

    state.event.Trigger s.request // TODO replace with new input in case it's needed
    let newConf = mng.getConfiguration ()
    let newGroups = currentGroups newConf originalGroups

    let newState =
      { state with
          conf = newConf
          groups = newGroups
          visibleControls = newGroups[groupIndex].settings |> Array.map settingToControl }

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

type NavigationHandler(mng: ConfigurationManager, requester: Event<Request>) =
  let mutable state =
    let conf = mng.getConfiguration ()
    let groups = currentGroups conf originalGroups

    { conf = conf
      groups = groups
      visibleControls = groups |> Array.map groupToControl
      activeGroup = None
      event = requester }

  // index: optional index of the item the user wants to activate
  // input: optional input text for the API key
  member _.activateAndGetSettings(index: int option, input: string option) : Control array =
    state <- activateSetting (state, mng) (index, input)

    state.visibleControls

  member _.completion(prompt: string) =
    Completion(LlmPrompt prompt) |> requester.Trigger

  member _.backToRoot() : Control array =
    state <- activateSetting ({ state with activeGroup = None }, mng) (None, None)
    state.visibleControls
