module ChatWindow

open Gtk
open Types
open MakeQuestion
open HandleCommands
open Controls

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)


let newChatWindow (providers: Map<string, ProviderAnswerers>) =
  let builder = new Builder("ChatWindow.glade")
  let rawWindow = builder.GetRawOwnedObject "ChatWindow"

  let window = new ChatWindow(rawWindow)

  let height = Gdk.Screen.Default.RootWindow.Height
  let width = Gdk.Screen.Default.RootWindow.Width
  window.HeightRequest <- height / 2
  window.WidthRequest <- width / 4
  window.Title <- "r0b0t"

  builder.Autoconnect window
  window.DeleteEvent.Add(fun _ -> Application.Quit())

  let di = newDisplayInput builder

  let showCommands = newShowCommands builder


  let conf =
    { providers = providers
      active =
        { provider = R0b0t.Provider.Github.providerName
          model = R0b0t.Provider.Github.defaultModel } }

  displayProviderModel builder conf.active.provider conf.active.model

  let mq () =
    let qa =
      { getQuestion = newGetQuestion di
        answerer = SelectProviderModel.getActiveAnswerer conf }

    let sa = newStopInsert di builder |> newStartAddText

    let sr = MakeQuestion.makeQuestion qa // connect later sr.stop with GUI
    ReadAnswer.readAnswer sa sr.read

  let cmds =
    { makeQuestion = mq
      showCommands = showCommands }

  confChatInputEvent di.chatInput (handleKeyPress cmds)

  confSend builder mq

  window
