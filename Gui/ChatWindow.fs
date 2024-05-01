module Chat

open Gtk
open Types
open MakeQuestion
open HandleCommands
open SelectProviderModel
open Controls

type ChatWindow(baseBuilder: nativeint) =
  inherit Window(baseBuilder)

let getImplementation (providers: Map<string, Provider>) (builder: Builder) =
  let config =
    { providers = providers
      active =
        { provider = R0b0t.Provider.Openai.providerName
          llm = R0b0t.Provider.Openai.defaultModel } }

  let selectors = newProviderLlm builder
  commandList builder
  confProviderSelectorUpdate config selectors

let readAnswer (builder: Builder) (di: DisplayInput) =
  let si = newStopInsert di builder

  let addText: ReadAnswer.StartAddText =
    { start = si.start
      addText =
        ReadAnswer.newAddText
          { stop = si.stop
            insertWord = si.insertWord } }

  ReadAnswer.readAnswer addText: ReadAnswer.ReadAnswer

let newChatWindow (providers: Map<string, Provider>) =
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


  //let getImp = getImplementation providers builder

  let mq () =
    let rs = newGetQuestion di |> makeQuestion2
    let si = newStopInsert di builder

    let addText: ReadAnswer.StartAddText =
      { start = si.start
        addText =
          ReadAnswer.newAddText
            { stop = si.stop
              insertWord = si.insertWord } }

    ReadAnswer.readAnswer2 addText rs

  let cmds =
    { makeQuestion = mq
      showCommands = showCommands }

  confChatInputEvent di.chatInput (handleKeyPress cmds)

  confSend builder mq

  window
