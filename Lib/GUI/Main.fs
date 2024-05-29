module GUI.Main

open Gtk
open GetProviderImpl
open InputOutput

let newConf () =
  let providers =
    [ ProviderModuleImpl.OpenAI.providerModule
      ProviderModuleImpl.GitHub.providerModule ]

  let _default = ProviderModuleImpl.OpenAI.providerModule.provider
  initConf providers _default

let displayProviderModel (b: Builder) (a: Active) =
  let providerL = b.GetObject "provider_label" :?> Label
  let modelL = b.GetObject "model_label" :?> Label

  providerL.Text <- a.provider
  modelL.Text <- a.model


let newWindow () =
  let builder = new Builder("GUI.glade")
  let w = newWindow builder
  let mutable conf = newConf ()


  displayProviderModel builder conf.active
  let io = newInputOutput w builder
  let getProvider = newGetProvider (fun _ -> conf) io.getPrompt
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  let showCommands = newShowCommands conf builder
  let hideCommands = newHideCommands builder

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
    | Gdk.Key.p when e.State.HasFlag Gdk.ModifierType.ControlMask -> showCommands ()
    | Gdk.Key.Escape -> hideCommands ()
    | _ -> ())

  w
