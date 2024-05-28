module GUI

open Gtk
open GetProviderImpl
open InputOutput

let newConf () =
  let providers =
    [ ProviderModuleImpl.OpenAI.providerModule
      ProviderModuleImpl.GitHub.providerModule ]

  let _default = ProviderModuleImpl.OpenAI.providerModule.provider
  initConf providers _default

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

let displayProviderModel (b: Builder) (a: Active) =
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  providerL.Text <- a.provider
  modelL.Text <- a.model


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

  conf <-
    { conf with
        active.model = OpenAI.ObjectModels.Models.Dall_e_3 }

  displayProviderModel builder conf.active
  let io = newInputOutput builder
  let getProvider = newGetProvider (fun _ -> conf) io.getPrompt
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  io.keyRelease (fun k ->
    let e = k.Event

    match e.Key with
    | Gdk.Key.Return when e.State.HasFlag Gdk.ModifierType.ControlMask ->
      answerSpinner.Start()

      let insertWord =
        match conf.active.model with
        | m when m = OpenAI.ObjectModels.Models.Dall_e_3 -> io.insertImage
        | _ -> io.insertWord

      Stream.Main.main
        getProvider
        { insertWord = insertWord
          stop = answerSpinner.Stop }
    | Gdk.Key.p when e.State.HasFlag Gdk.ModifierType.ControlMask -> ()
    | _ -> ())

  window
