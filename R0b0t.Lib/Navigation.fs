module Navigation

open System
open Configuration


type Control =
  | Checkbox of bool
  | Entry of string
  | SettingGroup of description: string

type Row = { label: string; control: Control }

type Prompt =
  | LlmPrompt of string
  | Introduction

type Request =
  | SetProvider of Provider
  | SetModel of Provider * Model
  | SetApiKey of Provider * Key
  | Completion of Prompt
  | Skip

type Setting = { row: Row; request: Request }

let settingGroups =
  [| { row =
         { label = "Set provider"
           control = SettingGroup "AI platform giving access to LLMs through an API" }
       request = Skip }
     { row =
         { label = "Set model"
           control = SettingGroup "Large Language Models available for the selected provider" }
       request = Skip }
     { row =
         { label = "Set API key"
           control = SettingGroup "API key for the selected provider for authorizing the requests" }
       request = Skip } |]

let providers = [| OpenAI; Anthropic; HuggingFace; GitHub; ImaginePro |]

let setProviderGroup =
  providers
  |> Array.map (fun p ->
    { row =
        { label = p.ToString()
          control = Checkbox false }
      request = SetProvider p })

let providersModels =
  [ OpenAI, openAIModels
    Anthropic, anthropicModels
    HuggingFace, huggingFaceModels
    GitHub, githubModels
    ImaginePro, imagineProAiModels ]


let setModelsGroup =
  providersModels
  |> List.map (fun (p, ms) ->
    let settings =
      ms
      |> List.map (fun m ->
        { row = { label = m; control = Checkbox false }
          request = SetModel(p, Model m) })
      |> List.toArray

    (p, settings))
  |> Map.ofList

let apiKeySetting provider (Key key) =
  [| { row =
         { label = "API key"
           control = Entry key }
       request = SetApiKey(provider, (Key key)) } |]


let newApiKeysGroup (providerKeys: Map<Provider, Key array>) = providerKeys |> Map.map apiKeySetting

// shows a list of groups
// given an index in case the current element is a group it shows the group members
// the group members can be fixed or depend on the activated element in another group
// given an index and maybe a string if the active element is a setting instead of a group
// it triggers the associated event passing the string as parameter if is needed by the event


// an array of functions that when activated, given the environment return a list of settings

type NavigationHandler(conf: Configuration, requester: Event<Request>) =
  let apiKeysGroup = newApiKeysGroup conf.keys

  let mutable activeGroup: int option = None

  // settings are contained in groups
  // which are identified by predefined indexes
  // active items inside groups are stored in groupActiveItem
  let mutable groupActiveItem =
    let activeProvider = Array.IndexOf(providers, conf.provider)

    let activeModels =
      providersModels |> List.find (fun (p, _) -> p = conf.provider) |> snd

    let (Model model) = conf.model
    let activeModelIndex = activeModels |> List.findIndex (fun m -> m = model)

    [| activeProvider, Checkbox true
       activeModelIndex, Checkbox true
       2, Entry "" |]

  let getGroupSettings group =
    let activeProvider = providers[fst groupActiveItem[0]]

    match group with
    | 0 -> setProviderGroup
    | 1 -> setModelsGroup[activeProvider]
    | 2 ->
      if apiKeysGroup.ContainsKey activeProvider then
        apiKeysGroup[activeProvider]
      else
        apiKeySetting activeProvider (Key "")

    | _ -> failwith $"mismatch between UI and NavigationHandler, setting group {group} out of range"

  // index: optional index of the item the user wants to activate
  // input: optional input text for the API key
  member _.activateAndGetSettings(index: int option, input: string option) : Row array =

    let showGroupRows activeGroup activeIndex =
      let content = groupActiveItem[activeGroup]

      match content, input with
      | (_, Checkbox _), _ -> groupActiveItem[activeGroup] <- (activeIndex, Checkbox true)
      | (_, Entry _), Some key -> groupActiveItem[activeGroup] <- (activeIndex, Entry key)
      | _ -> ()

      getGroupSettings activeGroup
      |> Array.mapi (fun j v ->
        let row = v.row

        if activeIndex = j then
          requester.Trigger v.request

          { row with
              control = snd groupActiveItem[activeGroup] }
        else
          row)

    match activeGroup, index with
    | None, Some i ->
      activeGroup <- index
      showGroupRows i (fst groupActiveItem[i])
    | Some g, Some i -> showGroupRows g i
    | _ -> settingGroups |> Array.map _.row

  member _.completion(prompt: string) =
    Completion(LlmPrompt prompt) |> requester.Trigger

  member _.backToRoot() = activeGroup <- None
