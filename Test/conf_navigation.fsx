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

type ModelIndex = uint
type ProviderIndex = uint

type ConfCommand =
  | SetModel of ModelIndex
  | SetProvider of ProviderIndex
  | SetApiKey of ProviderIndex * string

type ConfRoot = { name: string; description: string }

type ConfNavigator =
  { tree: Tree<ConfRoot, GuiControlPrototype * ConfCommand>
    currentLevelPath: uint list }

let currentLevel (c: ConfNavigator) =
  c.currentLevelPath
  |> List.fold
    (fun r i ->
      match r with
      | Node n -> n.children[int i]
      | Leaf x -> Leaf x)
    c.tree

let moveToChild (c: ConfNavigator) (index: uint) =
  { c with
      currentLevelPath = c.currentLevelPath @ [ index ] }
