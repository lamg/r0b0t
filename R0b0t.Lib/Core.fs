module Core

open FSharp.Control

open Configuration
open GtkGui
open Navigation

open ModelProvider

type MainBoxes =
  { topBar: Gtk.Box
    leftPanel: Gtk.ScrolledWindow
    rightPanel: Gtk.ScrolledWindow
    init: unit -> unit }

let setProvider (p: Provider) (conf: Configuration) =
  let model =
    providersModels |> List.find (fun (x, _) -> x = p) |> snd |> List.head |> Model

  { conf with
      provider = p
      model = model }

let welcomeMessage =
  let welcome = "# 🤖 Welcome!\n"
  let showCmdPalette = "- Press **Ctrl+p** for showing the command palette"

  let navigate =
    "- Navigate in the command palette using **Tab**, **Backspace**, **Enter**, **Arrow keys** and **Escape**"

  let sendPrompt = "- Send the prompt to the Language Model using **Ctrl+Enter**\n"

  [ welcome; sendPrompt; showCmdPalette; navigate ]
  |> List.map (fun s -> $"{s}\n".Split " " |> Array.toList)
  |> List.concat
  |> AsyncSeq.ofSeq
  |> AsyncSeq.mapAsync (fun w ->
    async {
      do! Async.Sleep 30
      return Word $"{w} "
    })

let startStreaming (producer: Event<LlmData>) comp =
  let finish _ = producer.Trigger End
  let reportExc = StreamExc >> producer.Trigger

  Async.StartWithContinuations(
    computation = comp,
    continuation = finish,
    exceptionContinuation = (reportExc >> finish),
    cancellationContinuation = finish
  )

let keyNotFoundMessage provider =
  [ Word $"key not found for {provider}" ] |> AsyncSeq.ofSeq

let setApiKeyMessage provider =
  [ Word $"API key set for {provider}\n" ] |> AsyncSeq.ofSeq

let streamMessage (producer: Event<LlmData>) (message: AsyncSeq<LlmData>) =
  producer.Trigger Prepare

  message |> AsyncSeq.iter producer.Trigger |> startStreaming producer

let streamCompletion provider key model prompt =
  match provider with
  | OpenAI when model = Model dalle3 -> OpenAI.imagine key prompt
  | OpenAI -> OpenAI.complete key model prompt
  | GitHub -> Github.ask key prompt
  | HuggingFace -> HuggingFace.ask key model prompt
  | Anthropic -> Anthropic.ask key model prompt
  | ImaginePro -> ImaginePro.imagine key prompt

let stream (conf: Configuration) (producer: Event<LlmData>) prompt =
  match prompt, Map.tryFind conf.provider conf.keys with
  | LlmPrompt prompt, Some key -> streamCompletion conf.provider key conf.model prompt
  | LlmPrompt _, None -> keyNotFoundMessage conf.provider
  | Introduction, _ -> welcomeMessage
  |> streamMessage producer

let main () =
  let producerEvent = Event<LlmData>()
  let requestEvent = Event<Request>()
  let mng = ConfigurationManager()
  mng.loadConfiguration ()
  let nav = NavigationHandler(mng.getConfiguration (), requestEvent)
  let r = rightPanel nav

  let updateTopBar () =
    let { model = (Model model)
          provider = provider } =
      mng.getConfiguration ()

    r.topBar.model.SetText model
    r.topBar.provider.SetText(provider.ToString())

  let mutable isStreaming = false

  let onLlmData =
    function
    | End -> isStreaming <- false
    | _ -> ()

  let onRequest (request: Request) =
    match request with
    | Skip -> ()
    | SetModel(_, m) ->
      let c =
        { mng.getConfiguration () with
            model = m }

      mng.setConfiguration c
      updateTopBar ()
    | SetProvider p ->
      mng.getConfiguration () |> setProvider p |> mng.setConfiguration
      updateTopBar ()
    | Completion prompt when not isStreaming ->
      isStreaming <- true
      stream (mng.getConfiguration ()) producerEvent prompt
    | Completion _ -> ()
    | SetApiKey(_, Key s) ->
      let key = s.Trim()
      let c = mng.getConfiguration ()

      let c =
        { c with
            keys = Map.add c.provider (Key key) c.keys }

      mng.setConfiguration c

      streamMessage producerEvent (setApiKeyMessage c.provider)

  requestEvent.Publish.Add onRequest
  producerEvent.Publish.Add onLlmData

  let left = leftPanel r.topBar.spinner producerEvent.Publish

  { leftPanel = left
    rightPanel = r.rightPanel
    topBar = r.topBar.box
    init =
      fun _ ->
        updateTopBar ()
        requestEvent.Trigger(Completion Introduction) }
