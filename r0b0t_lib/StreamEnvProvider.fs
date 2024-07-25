module r0b0tLib.StreamEnvProvider

open Gtk
open Gdk
open GdkPixbuf

open Core
open Controls
open CommandPalette

type StreamEnvProvider(controls: Controls) =
  let eventSource = Event<Request>()

  let mutable conf =
    { model = Model(Gpt4o.ToString())
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

  let onActivateItem (l: ListBox) (e: ListBox.RowActivatedSignalArgs) =
    let current = e.Row.GetIndex() |> controls.navigationHandler.moveToChild

    match current with
    | [ Leaf(_, cmd) ] ->
      // trigger event with command
      ()
    | [ Node { value = v; children = chl } ] ->
      // replace current list elements by chl
      ()
    | _ -> failwith "todo"

    printfn $"activated {e.Row.Child.Name} row"

  let onCtrlEnterSendPrompt (_: EventControllerKey) (e: EventControllerKey.KeyReleasedSignalArgs) =
    match e.State, e.Keycode with
    | ModifierType.ControlMask, 36ul ->
      controls.leftSrc.Buffer.Text <- ""
      controls.rightSrc.Buffer.Text |> Prompt |> Completion |> eventSource.Trigger
    | ModifierType.ControlMask, 27ul ->
      // control + p
      controls.rightSrc.Hide()
      controls.confBox.Show()
    | ModifierType.NoModifierMask, 9ul ->
      // escape
      controls.confBox.Hide()
      controls.rightSrc.Show()
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


  member this.loadConfiguration() =
    // TODO load from storage and deserialize
    conf

  member this.storeConfiguration c =
    // TODO serialize and store
    conf <- c
    updateControls ()

  member this.streamCompletion provider key model prompt =
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

      member _.consume data = m.consume data }
