module CommandPalette

open System
open Gtk
open Core

type Node<'a, 'b> =
  { value: 'a
    children: Tree<'a, 'b> array }

and Tree<'a, 'b> =
  | Node of Node<'a, 'b>
  | Leaf of 'b

type InputType =
  | Bool of bool
  | String of string
  | Natural of uint
  | Integer of int
  | Float of float

type GuiControlPrototype = { inputType: InputType; name: string }
type ConfRoot = { name: string; description: string }
type Setting = GuiControlPrototype * Request

let childrenAt tree (path: uint list) =
  path
  |> List.fold
    (fun r i ->
      match r with
      | Node n -> n.children[int i]
      | Leaf x -> Leaf x)
    tree
  |> function
    | Node n -> n.children
    | x -> [| x |]

let rec replaceChildren tree pred children =
  match tree with
  | Node { value = v } when pred v -> Node { value = v; children = children }
  | Node { value = v; children = xs } ->
    Node
      { value = v
        children = xs |> Array.map (fun x -> replaceChildren x pred children) }
  | _ -> tree

let rec mapLeafs tree f =
  match tree with
  | Node v ->
    Node
      { v with
          children = v.children |> Array.map (fun x -> mapLeafs x f) }
  | Leaf v -> Leaf(f v)

let setProviderTree =
  Node
    { value =
        { name = "Set provider"
          description = "Set which LLM service will handle the requests" }
      children =
        [| Leaf(
             { name = "OpenAI"
               inputType = Bool false },
             SetProvider OpenAI
           )
           Leaf(
             { name = "Anthropic"
               inputType = Bool false },
             SetProvider Anthropic
           )
           Leaf(
             { name = "GitHub"
               inputType = Bool false },
             SetProvider GitHub
           )
           Leaf(
             { name = "HuggingFace"
               inputType = Bool false },
             SetProvider HuggingFace
           )
           Leaf(
             { name = "ImaginePro"
               inputType = Bool false },
             SetProvider ImaginePro
           ) |] }


[<Literal>]
let setModelName = "Set model"

let createModelChildren (xs: string list) =
  xs
  |> List.map (fun m -> Leaf({ name = m; inputType = Bool false }, SetModel(Model m)))
  |> List.toArray

let setModelTree =
  Node
    { value =
        { name = setModelName
          description = "Set which specific LLM will handle requests in the selected provider" }
      children = [||] }

let setProviderModels models =
  models
  |> List.map (fun x -> Leaf({ name = x; inputType = Bool false }, SetModel(Model x)))
  |> List.toArray

let setOpenAIModels: Tree<ConfRoot, Setting> array =
  openAIModels |> setProviderModels

let setAnthropicModels: Tree<ConfRoot, Setting> array =
  anthropicModels |> setProviderModels

let setHuggingFaceModels: Tree<ConfRoot, Setting> array =
  huggingFaceModels |> setProviderModels

let setGithubModels: Tree<ConfRoot, Setting> array =
  githubModels |> setProviderModels

let setImagineProModels: Tree<ConfRoot, Setting> array =
  imagineProAiModels |> setProviderModels

let modelsForProvider p =
  let xs =
    [ OpenAI, setOpenAIModels
      Anthropic, setAnthropicModels
      HuggingFace, setHuggingFaceModels
      GitHub, setGithubModels
      ImaginePro, setImagineProModels ]

  xs |> List.find (fun (x, _) -> x = p) |> snd

let setApiKeyTree =
  Node
    { value =
        { name = "Set API key"
          description = "Set the API key to authorize this client to make requests to the selected provider" }
      children = [||] }

let setFont =
  Node
    { value =
        { name = "Set font"
          description = "Font settings for all controls" }
      // children = [| setLeftFont; setRightFont; setTextViewFont |] }
      children = [||] }

let root =
  Node
    { value = { name = "root"; description = "root" }
      children = [| setProviderTree; setModelTree; setApiKeyTree; setFont |] }

type NavigationHandler() =
  let mutable tree = root

  let mutable currentLevelPath = []
  let mutable currentFilteredPaths = []

  member _.getCurrentItems() : Tree<ConfRoot, Setting> array = childrenAt tree currentLevelPath

  member this.filterTree(text: string) =
    let lowerText = text.ToLower()

    this.getCurrentItems ()
    |> Array.filter (function
      | Leaf({ name = name }, _) -> name.ToLower().Contains lowerText
      | Node { value = v } ->
        let lowerName = v.name.ToLower()
        let lowerDescription = v.description.ToLower()
        lowerName.Contains lowerText || lowerDescription.Contains lowerText)

  member _.replaceChildren pred xs = tree <- replaceChildren tree pred xs

  member _.moveToChild index =
    currentLevelPath <- index :: currentLevelPath

  member _.backToRoot() = currentLevelPath <- []

  member _.activateLeafs xs =
    tree <-
      mapLeafs tree (function
        | { name = name; inputType = Bool b } as x, y ->
          ({ x with
              inputType = Bool(List.contains name xs) },
           y)
        | r -> r)

let settingLabel (text: string) =
  let l = new Label()
  l.SetText text
  l.SetHalign Align.Start
  l.SetValign Align.Start
  l.SetHexpand true
  l.SetVexpand true
  l.SetWrap true
  l.SetMarginBottom 10
  l.AddCssClass "setting-label"
  l

let boolSetting text value =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  settingLabel text |> box.Append
  let check = new CheckButton()
  check.Active <- value
  check |> box.Append

  box

let settingGroup g =
  let mainLabel = new Label()
  mainLabel.SetText g.name
  mainLabel.SetHalign Align.Start
  mainLabel.SetValign Align.Start
  mainLabel.SetHexpand true
  mainLabel.SetVexpand true
  mainLabel.SetWrap true
  mainLabel.SetMarginBottom 10
  mainLabel.AddCssClass "main"

  let descrLabel = new Label()
  descrLabel.SetText g.description
  descrLabel.SetEllipsize Pango.EllipsizeMode.End
  descrLabel.SetHalign Align.Start
  descrLabel.SetValign Align.End
  descrLabel.SetHexpand true
  descrLabel.SetVexpand false
  descrLabel.SetMarginBottom 10
  descrLabel.AddCssClass "description"

  let item = new Box()
  item.Name <- g.name

  item.SetOrientation Orientation.Vertical
  item.Append mainLabel
  item.Append descrLabel
  item

let populateListBox (l: ListBox) (xs: Tree<ConfRoot, Setting> array) =
  let appendLeaf (proto, cmd) =
    match proto.inputType with
    | Bool v -> boolSetting proto.name v |> l.Append
    | String _ -> boolSetting proto.name false |> l.Append
    | Natural _ -> boolSetting proto.name false |> l.Append
    | Integer _ -> boolSetting proto.name false |> l.Append
    | Float _ -> boolSetting proto.name false |> l.Append

  let appendNode r = r |> settingGroup |> l.Append
  l.RemoveAll()

  xs
  |> Array.iter (function
    | Leaf x -> appendLeaf x
    | Node y -> appendNode y.value)


let onSearchChanged (nav: NavigationHandler, l: ListBox) (s: SearchEntry) (_: EventArgs) =
  l.RemoveAll()
  s.GetText() |> nav.filterTree |> populateListBox l

let configurationBox (nav: NavigationHandler) =
  let box = new Box()
  box.SetOrientation Orientation.Vertical
  box.SetHomogeneous false
  let s = new SearchEntry()
  let l = new ListBox()

  s.add_OnSearchChanged (GObject.SignalHandler<SearchEntry>(onSearchChanged (nav, l)))
  nav.getCurrentItems () |> populateListBox l

  box.Append s
  box.Append l
  box, s, l
