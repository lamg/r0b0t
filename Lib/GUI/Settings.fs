module GUI.Settings

open Gtk
open GetProviderImpl

let newConf () =
  let providers =
    [ ProviderModuleImpl.OpenAI.providerModule
      ProviderModuleImpl.GitHub.providerModule
      ProviderModuleImpl.Anthropic.providerModule
      ProviderModuleImpl.HuggingFace.providerModule ]

  let _default = ProviderModuleImpl.OpenAI.providerModule.provider

  initConf providers _default


type ConfHandler =
  { setConf: Conf -> unit
    getConf: unit -> Conf }

let newConfHandler () =
  let mutable conf = newConf ()
  let setConf (c: Conf) = conf <- c
  let getConf () = conf
  { setConf = setConf; getConf = getConf }

let inspectCombo (combo: ComboBoxText) =
  let listStore = combo.Model :?> ListStore
  let mutable iter = TreeIter()
  let mutable isValid = listStore.GetIterFirst(&iter) // Get the first item
  // Iterate through all items
  while isValid do
    let value = listStore.GetValue(iter, 0) :?> string
    printfn $"Item: {value}"
    isValid <- listStore.IterNext(ref iter)

let displayProviderModel (b: Builder) (a: Active) =
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  providerL.Text <- a.provider
  modelL.Text <- a.model

let settingBox (name: string) (active: string) (values: string seq) (changed: string -> unit) =
  let box = new Box(Orientation.Horizontal, 2)
  let label = new Label(name)
  label.CanFocus <- false
  box.PackStart(label, false, true, 5u)
  let combo = new ComboBoxText()
  values |> Seq.iter combo.AppendText
  let eq x y = x = y
  combo.Active <- values |> Seq.findIndex (eq active)
  box.PackStart(combo, true, true, 0u)
  combo.Changed.Add(fun _ -> changed combo.ActiveText)

  box,
  (fun (active: string) (items: string list) ->
    combo.RemoveAll()
    items |> Seq.iter combo.AppendText
    let activeIndex = items |> Seq.findIndex (eq active)
    combo.Active <- activeIndex)

let changedModel (modelLabel: Label) ch active =
  let conf = ch.getConf ()

  if validModelState conf active then
    setActiveModel conf active |> ch.setConf
    modelLabel.Text <- active

let changedProvider (providerLabel: Label) (modelLabel: Label) ch updateModels active =
  let conf = ch.getConf ()

  if validProviderState conf active then
    setActiveProvider conf active |> ch.setConf
    let conf = ch.getConf ()
    updateModels conf.active.model conf.providers[conf.active.provider].models
    providerLabel.Text <- conf.active.provider
    modelLabel.Text <- conf.active.model


let newShowCommands (ch: ConfHandler) (b: Builder) =
  let commandList = b.GetObject "command_list" :?> ListBox
  let commandSearch = b.GetObject "command_search" :?> SearchEntry
  let commandBox = b.GetObject "command_box" :?> Box
  let providerLabel = b.GetObject "provider_label" :?> Label
  let modelLabel = b.GetObject "model_label" :?> Label

  let conf = ch.getConf ()

  let modelSetting, updateModels =
    settingBox "Models" conf.active.model conf.providers[conf.active.provider].models (changedModel modelLabel ch)

  let providerSetting, _ =
    let conf = ch.getConf ()

    settingBox
      "Providers"
      conf.active.provider
      conf.providers.Keys
      (changedProvider providerLabel modelLabel ch updateModels)

  commandList.Add modelSetting
  commandList.Add providerSetting

  commandBox.Expand <- true
  commandList.ShowAll()

  fun () ->
    commandSearch.GrabFocus()
    commandBox.Visible <- true


let newHideCommands (b: Builder) =
  let commandBox = b.GetObject "command_box" :?> Box
  let chatInput = b.GetObject "chat_input" :?> TextView

  fun () ->
    chatInput.GrabFocus()
    commandBox.Visible <- false
