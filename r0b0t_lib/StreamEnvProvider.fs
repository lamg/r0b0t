module r0b0tLib.StreamEnvProvider

open FSharp.Control
open Gtk
open Gdk
open GdkPixbuf

open Core
open Controls
open CommandPalette

type StreamEnvProvider(controls: Controls) =
  let eventSource = Event<Request>()

  let mutable conf =
    { model = Model(Gpt4oMini.ToString())
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
    | :? System.ArgumentException when e.Message.Contains "The input sequence was empty" -> ()
    | _ -> printfn $"{e.GetType().ToString()} msg = {e.Message}"

    controls.spinner.Stop()

  member this.loadConfiguration() =
    // TODO load from storage and deserialize
    conf

  member this.storeConfiguration c =
    // TODO serialize and store
    conf <- c
    updateControls ()

  member this.streamCompletion provider key model prompt =
    controls.spinner.Start()

    match provider with
    | OpenAI when model = Model(Dalle3.ToString()) ->
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
