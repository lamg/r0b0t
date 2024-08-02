module r0b0tLib.StreamEnvProvider

open System
open FSharp.Control

open Gtk
open Gdk
open GdkPixbuf

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png.Chunks

open Core
open Controls
open CommandPalette

type SerializableConf =
  { provider_keys: Map<string, string>
    model: string
    provider: string }

let controlEnter = ModifierType.ControlMask, 36ul
let controlP = ModifierType.ControlMask, 27ul
let escape = ModifierType.NoModifierMask, 9ul
let backspace = ModifierType.NoModifierMask, 22ul
let downArrow = ModifierType.NoModifierMask, 116ul

[<Literal>]
let promptPngMetadataKey = "prompt"

[<Literal>]
let revisedPromptPngMetadataKey = "revised_prompt"

let saveImage (d: PngData) =
  use image = Image.Load d.image
  let pngMeta = image.Metadata.GetPngMetadata()
  pngMeta.TextData.Add(PngTextData(promptPngMetadataKey, d.prompt, "en", promptPngMetadataKey))
  pngMeta.TextData.Add(PngTextData(revisedPromptPngMetadataKey, d.revisedPrompt, "en", revisedPromptPngMetadataKey))
  let now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss")
  let outputPath = $"img_{now}.png"
  image.Save(outputPath)

type StreamEnvProvider(controls: Controls) =
  let eventSource = Event<Request>()

  let mutable conf =
    { model = Model gpt4oMini
      provider = OpenAI
      keys =
        [ OpenAI, "openai_key"
          Anthropic, "anthropic_key"
          HuggingFace, "huggingface_key"
          GitHub, "github_key" ]
        |> List.choose (fun (p, var) ->
          match LamgEnv.getEnv var with
          | Some k -> Some(p, Key k)
          | _ -> None)
        |> Map.ofList }

  let onActivateItem (lb: ListBox) (e: ListBox.RowActivatedSignalArgs) =
    let index = e.Row.GetIndex()
    let items = controls.navigationHandler.getCurrentItems ()
    let current = items |> Array.item index

    let setBoolRow v (r: ListBoxRow) =
      let box = r.Child :?> Box
      let check = box.GetLastChild() :?> CheckButton
      check.Active <- v

    let switchOffCheckButtons butIndex =
      for i in 0 .. items.Length - 1 do
        match items[i] with
        | Leaf({ inputType = Bool _ }, _) -> lb.GetRowAtIndex i |> setBoolRow (i = butIndex)
        | _ -> ()

    match current with
    | Leaf(prototype, cmd) ->
      eventSource.Trigger cmd

      match prototype with
      | { inputType = Bool _ } -> switchOffCheckButtons index
      | _ -> ()
    | Node { value = _; children = xs } ->
      index |> uint |> controls.navigationHandler.moveToChild
      populateListBox lb xs

  let onCtrlEnterSendPrompt (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    let keys = e.State, e.Keycode

    match keys with
    | _ when keys = controlEnter ->
      controls.leftSrc.Buffer.Text <- ""
      controls.rightSrc.Buffer.Text |> Prompt |> Completion |> eventSource.Trigger
    | _ when keys = controlP ->
      // control + p
      controls.rightSrc.Hide()
      controls.confBox.Show()
      controls.searchConf.GrabFocus() |> ignore
    | _ -> ()

  let onEscHideConfBox (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    let keys = e.State, e.Keycode

    match keys with
    | _ when keys = escape ->
      controls.confBox.Hide()
      controls.rightSrc.Show()
      controls.rightSrc.GrabFocus() |> ignore
    | _ when keys = controlP -> controls.searchConf.GrabFocus() |> ignore
    | _ when keys = backspace ->
      controls.navigationHandler.backToRoot ()

      controls.navigationHandler.getCurrentItems ()
      |> populateListBox controls.listBox
    | _ when keys = downArrow && controls.searchConf.HasFocus -> controls.listBox.GetFirstChild().GrabFocus() |> ignore
    | _ -> ()

  let confPath =
    (LamgEnv.getEnv "HOME" |> Option.defaultValue "~")
    :: [ ".config"; "r0b0t.json" ]
    |> List.toArray
    |> System.IO.Path.Join

  do
    let ctrlPController = EventControllerKey.New()

    ctrlPController.add_OnKeyReleased (
      GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onCtrlEnterSendPrompt
    )

    controls.rightBox.AddController ctrlPController

    let ctrlEnterController = EventControllerKey.New()

    ctrlEnterController.add_OnKeyReleased (
      GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onCtrlEnterSendPrompt
    )

    controls.rightSrc.AddController ctrlEnterController
    controls.listBox.add_OnRowActivated (GObject.SignalHandler<ListBox, ListBox.RowActivatedSignalArgs> onActivateItem)

    let escController = EventControllerKey.New()

    escController.add_OnKeyReleased (
      GObject.SignalHandler<EventControllerKey, EventControllerKey.KeyReleasedSignalArgs> onEscHideConfBox
    )

    controls.confBox.AddController escController
    controls.listBox.AddController escController

  member _.TriggerEvent(request: Request) = eventSource.Trigger(request)

  member _.event = eventSource.Publish

  member _.consume(d: LlmData) =
    async {
      GLib.Functions.IdleAdd(
        int GLib.ThreadPriority.Normal,
        fun _ ->
          match d with
          | Word w ->
            if not controls.leftSrc.Visible then
              controls.leftSrc.Show()
              controls.picture.Hide()

            controls.leftSrc.Buffer.Text <- $"{controls.leftSrc.Buffer.Text}{w}"
          | PngData img ->
            if not controls.picture.Visible then

              controls.leftSrc.Hide()
              controls.picture.Show()

            let p = PixbufLoader.FromBytes img.image
            controls.picture.SetPixbuf p
            saveImage img

          false
      )
      |> ignore
    }

  member this.consumptionEnd() =
    GLib.Functions.IdleAdd(
      int GLib.ThreadPriority.Urgent,
      fun _ ->
        controls.spinner.Stop()
        false
    )
    |> ignore

  member this.consumeException(e: exn) =
    match e with
    | :? ArgumentException when e.Message.Contains "The input sequence was empty" -> ()
    | _ ->
      GLib.Functions.IdleAdd(
        int GLib.ThreadPriority.Urgent,
        fun _ ->
          controls.leftSrc.Buffer.Text <- $"{e.GetType().ToString()} msg = {e.Message}"
          false
      )
      |> ignore

    this.consumptionEnd ()

  member this.loadConfiguration() =
    if System.IO.File.Exists confPath then
      try
        confPath
        |> IO.File.ReadAllText
        |> Text.Json.JsonSerializer.Deserialize<SerializableConf>
        |> function
          | { model = model
              provider = provider
              provider_keys = pks } ->

            let pks =
              try
                pks.Count |> ignore
                pks
              with _ ->
                Map.empty // a hack to handle the null map, which cannot be handled with Option.ofObj

            let keys =
              providersModels
              |> List.choose (fun (p, _) ->
                match Map.tryFind (p.ToString()) pks with
                | Some key -> Some(p, Key key)
                | None -> None)
              |> Map.ofList

            providersModels
            |> List.tryFind (fun (p, _) -> p.ToString() = provider)
            |> function
              | Some(p, models) when models |> List.exists (fun x -> x = model) ->
                let mergedKeys = conf.keys |> Map.fold (fun m k v -> Map.add k v m) keys

                conf <-
                  { provider = p
                    model = Model model
                    keys = mergedKeys }
              | Some(p, _) -> eprintfn $"model {model} loaded but not supported by {p}"
              | None -> eprintfn $"provider {provider} loaded from configuration, but not supported"
      with e ->
        eprintfn $"failed to load configuration: {e.Message}"
    else
      this.storeConfiguration ()

  member this.storeConfiguration() =
    try
      let (Model model) = conf.model
      let provider = conf.provider.ToString()

      let keys =
        conf.keys
        |> Map.toList
        |> List.map (fun (p, Key k) -> p.ToString(), k)
        |> Map.ofList

      { provider = provider
        model = model
        provider_keys = keys }
      |> (fun v ->
        let opts = Text.Json.JsonSerializerOptions(WriteIndented = true)
        Text.Json.JsonSerializer.Serialize<SerializableConf>(v, opts))
      |> fun json -> IO.File.WriteAllText(confPath, json)
    with e ->
      eprintfn $"failed to store configuration: {e.Message}"

  member _.getConfiguration() = conf

  member _.setConfiguration c =
    conf <- c

    modelsForProvider c.provider
    |> controls.navigationHandler.replaceChildren (fun cr -> cr.name = setModelName)

    controls.navigationHandler.activateLeafs
      [ conf.provider.ToString()
        conf.model
        |> function
          | Model m -> m ]

    conf.provider.ToString() |> controls.providerLabel.SetText
    conf.model.ToString() |> controls.modelLabel.SetText

  member _.streamCompletion provider key model prompt =
    GLib.Functions.IdleAdd(
      int GLib.ThreadPriority.Urgent,
      fun _ ->
        controls.spinner.Start()
        false
    )
    |> ignore

    match provider with
    | OpenAI when model = Model dalle3 ->
      // TODO show to the user a timer with the timeout as upper limit
      OpenAI.imagine key prompt
    | OpenAI -> OpenAI.complete key model prompt
    | GitHub -> Github.ask key prompt
    | HuggingFace -> failwith "todo"
    | Anthropic -> failwith "todo"
    | ImaginePro -> failwith "todo"

let newStreamEnv (c: Controls) =
  let m = StreamEnvProvider c

  { new StreamEnv with
      member _.event = m.event
      member _.storeConfiguration() = m.storeConfiguration ()
      member _.loadConfiguration() = m.loadConfiguration ()
      member _.getConfiguration() = m.getConfiguration ()
      member _.setConfiguration c = m.setConfiguration c

      member _.streamCompletion provider key model prompt =
        m.streamCompletion provider key model prompt

      member _.consume data = m.consume data
      member _.consumptionEnd() = m.consumptionEnd ()
      member _.consumeException e = m.consumeException e }
