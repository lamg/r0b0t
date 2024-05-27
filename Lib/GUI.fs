module GUI

open System
open Gtk
open GetProviderImpl


type InputOutput =
  { getPrompt: unit -> Prompt
    keyRelease: (KeyReleaseEventArgs -> unit) -> unit
    insertWord: string -> unit }

let newInputOutput (b: Builder) : InputOutput =
  let chatDisplay = b.GetObject "chat_display" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatDisplay.StyleContext.AddProvider(provider, 0u)

  let chatInput = b.GetObject "chat_input" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatInput.StyleContext.AddProvider(provider, 0u)

  let adjustment = b.GetObject "text_adjustment" :?> Adjustment

  { getPrompt =
      fun () ->
        let question = chatInput.Buffer.Text

        chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
        chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
        question

    keyRelease = chatInput.KeyReleaseEvent.Add
    insertWord =
      (function
      | w when w = String.Empty || isNull w -> ()
      | w ->
        GLib.Idle.Add(fun _ ->
          chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
          chatDisplay.Buffer.InsertAtCursor w
          adjustment.Value <- adjustment.Upper
          false)
        |> ignore) }

let newConf () =
  let providers =
    [ ProviderModuleImpl.OpenAI.providerModule
      ProviderModuleImpl.GitHub.providerModule ]

  let _default = ProviderModuleImpl.OpenAI.providerModule.provider
  initConf providers _default

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

let newWindow () =
  let builder = new Builder("GUI.glade")
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)

  let height = Gdk.Screen.Default.RootWindow.Height
  let width = Gdk.Screen.Default.RootWindow.Width
  window.HeightRequest <- height / 2
  window.WidthRequest <- width / 4
  window.Title <- "r0b0t"

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())


  let mutable conf = newConf ()
  let io = newInputOutput builder
  let getProvider = newGetProvider (fun _ -> conf) io.getPrompt
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  io.keyRelease (fun k ->
    let e = k.Event

    match e.Key with
    | Gdk.Key.Return when e.State.HasFlag Gdk.ModifierType.ControlMask ->
      answerSpinner.Start()

      Stream.Main.main
        getProvider
        { insertWord = io.insertWord
          stop = answerSpinner.Stop }

    | Gdk.Key.p when e.State.HasFlag Gdk.ModifierType.ControlMask -> ()
    | _ -> ()

  )

  window
