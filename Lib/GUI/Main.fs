module GUI.Main

open Gtk

open GetProviderImpl
open Stream.Types

open InputOutput
open Settings

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

let newWindow (builder: Builder) =
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)
  window.Maximize()
  window.Title <- "r0b0t"

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())
  window

let newAppWindow () =
  let builder = new Builder("GUI.glade")
  let w = newWindow builder
  let confHandler = newConfHandler ()

  displayProviderModel builder (confHandler.getConf().active)
  let io = newInputOutput w builder

  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  let showCommands = newShowCommands confHandler builder
  let hideCommands = newHideCommands builder

  io.keyRelease (fun k ->
    let e = k.Event

    match e.Key with
    | Gdk.Key.Return when e.State.HasFlag Gdk.ModifierType.ControlMask ->
      answerSpinner.Start()

      let insertData =
        function
        | Word w -> io.insertWord w
        | PngBase64 i -> io.insertImage i

      let getProvider = newGetProvider (confHandler.getConf ()) io.getPrompt

      Stream.Main.main
        getProvider
        { insertData = insertData
          stop =
            (fun r ->
              match r with
              | Done -> ()
              | Timeout -> ()

              answerSpinner.Stop()) }
    | Gdk.Key.p when e.State.HasFlag Gdk.ModifierType.ControlMask -> showCommands ()
    | Gdk.Key.Escape -> hideCommands ()
    | _ -> ())

  w
