module r0b0tLib.StreamEnvProvider

open System
open FSharp.Control
open Gtk
open Gdk
open GdkPixbuf

open Core
open Controls
open CommandPalette

type SerializableConf =
  { provider_keys: Map<string, string>
    model: string
    provider: string }

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

  let onActivateItem (_: ListBox) (e: ListBox.RowActivatedSignalArgs) =
    let current = e.Row.GetIndex() |> controls.navigationHandler.moveToChild

    match current with
    | Leaf(_, cmd) ->
      // trigger event with command
      printfn $"trigger {cmd}"
      ()
    | Node { value = v; children = chl } ->
      printfn $"moving to children of {v}"
      // replace current list elements by chl
      ()

    printfn $"activated {e.Row.Child.Name} row"

  let onCtrlEnterSendPrompt (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    match e.State, e.Keycode with
    | ModifierType.ControlMask, 36ul ->
      // control + enter
      controls.leftSrc.Buffer.Text <- ""
      controls.rightSrc.Buffer.Text |> Prompt |> Completion |> eventSource.Trigger
    | ModifierType.ControlMask, 27ul ->
      // control + p
      controls.rightSrc.Hide()
      controls.confBox.Show()
      controls.searchConf.GrabFocus() |> ignore
    | _ -> ()

  let onEscHideConfBox (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    match e.State, e.Keycode with
    | ModifierType.NoModifierMask, 9ul ->
      // escape
      controls.confBox.Hide()
      controls.rightSrc.Show()
      controls.rightSrc.GrabFocus() |> ignore
    | ModifierType.ControlMask, 27ul -> controls.searchConf.GrabFocus() |> ignore
    | _ -> ()

  let updateControls () =
    conf.provider.ToString() |> controls.providerLabel.SetText
    conf.model.ToString() |> controls.modelLabel.SetText

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

    updateControls ()

  member _.TriggerEvent(request: Request) = eventSource.Trigger(request)

  member _.event = eventSource.Publish

  member _.consume(d: LlmData) =
    GLib.Functions.IdleAdd(
      int GLib.ThreadPriority.Urgent,
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

        false
    )
    |> ignore

  member _.consumptionEnd() = controls.spinner.Stop()

  member _.consumeException(e: exn) =
    match e with
    | :? ArgumentException when e.Message.Contains "The input sequence was empty" -> ()
    | _ -> printfn $"{e.GetType().ToString()} msg = {e.Message}"

    controls.spinner.Stop()

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

            let providers =
              [ OpenAI, openAIModels
                GitHub, githubModels
                HuggingFace, huggingFaceModels
                Anthropic, anthropicModels ]

            let keys =
              providers
              |> List.choose (fun (p, _) ->
                match Map.tryFind (p.ToString()) pks with
                | Some key -> Some(p, Key key)
                | None -> None)
              |> Map.ofList

            providers
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
      this.storeConfiguration conf

    conf

  member this.storeConfiguration c =
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

    conf <- c
    updateControls ()

  member this.streamCompletion provider key model prompt =
    controls.spinner.Start()

    match provider with
    | OpenAI when model = Model dalle3 ->
      // TODO show to the user a timer with the timeout as upper limit
      OpenAI.imagine key prompt
    | OpenAI -> OpenAI.complete key model prompt
    | GitHub -> failwith "todo"
    | HuggingFace -> failwith "todo"
    | Anthropic -> failwith "todo"

let newStreamEnv (c: Controls) =
  let m = StreamEnvProvider c

  { new StreamEnv with
      member _.event = m.event
      member _.storeConfiguration c = m.storeConfiguration c
      member _.loadConfiguration() = m.loadConfiguration ()

      member _.streamCompletion provider key model prompt =
        m.streamCompletion provider key model prompt

      member _.consume data = m.consume data
      member _.consumptionEnd() = m.consumptionEnd ()
      member _.consumeException e = m.consumeException e }
