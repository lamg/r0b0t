module r0b0tLib.CommandPalette

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
  | Bool
  | String
  | Natural
  | Integer
  | Float

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

let setProviderTree =
  Node
    { value =
        { name = "Set provider"
          description = "Set which LLM service will handle the requests" }
      children =
        [| Leaf({ name = "OpenAI"; inputType = Bool }, SetProvider OpenAI)
           Leaf({ name = "Anthropic"; inputType = Bool }, SetProvider Anthropic)
           Leaf({ name = "GitHub"; inputType = Bool }, SetProvider GitHub)
           Leaf(
             { name = "HuggingFace"
               inputType = Bool },
             SetProvider HuggingFace
           ) |] }

let setModelTree =
  Node
    { value =
        { name = "Set model"
          description = "Set which specific LLM will handle requestes in the selected provider" }
      children = [||] }

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
  let tree = root

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

  member _.moveToChild index =
    currentLevelPath <- index :: currentLevelPath

  member _.backToRoot() = currentLevelPath <- []

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

let settingCheckBox () =
  let check = new CheckButton()
  //check.add_OnToggled (GObject.SignalHandler<CheckButton> onToggled)
  check

let boolSetting text =
  let box = new Box()
  box.SetOrientation Orientation.Horizontal
  settingLabel text |> box.Append
  settingCheckBox () |> box.Append

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
    | Bool -> boolSetting proto.name |> l.Append
    | String -> boolSetting proto.name |> l.Append
    | Natural -> boolSetting proto.name |> l.Append
    | Integer -> boolSetting proto.name |> l.Append
    | Float -> boolSetting proto.name |> l.Append

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
