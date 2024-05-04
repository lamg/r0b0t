

namespace FSharp


module Controls

type DisplayInput =
    {
      chatDisplay: Gtk.TextView
      chatInput: Gtk.TextView
    }

type StartStop =
    {
      start: (unit -> unit)
      stop: (unit -> unit)
    }

type InsertWord = string -> unit

type AdjustWord =
    {
      displayInput: DisplayInput
      adjustment: Gtk.Adjustment
    }

type SetProviderModel = Types.Provider -> Types.Model -> unit

type GetQuestion = unit -> string

type ClickSubscriber = (unit -> unit) -> unit

type ShowCommands = unit -> unit

type KeyEventSubscriber = (Gdk.EventKey -> unit) -> unit

val newAdjustWord: di: DisplayInput -> b: Gtk.Builder -> AdjustWord

val insertWord: AdjustWord -> InsertWord

val newGetQuestion: di: DisplayInput -> GetQuestion

type StartStopInsert =
    {
      stop: (unit -> unit)
      start: (unit -> unit)
      insertWord: InsertWord
    }

val newStopInsert: di: DisplayInput -> builder: Gtk.Builder -> StartStopInsert

type StopInsert =
    {
      stop: (unit -> unit)
      insertWord: (string -> unit)
    }

val newAddText: stopInsert: StopInsert -> word: string option -> unit

type StartAddText =
    {
      start: (unit -> unit)
      addText: (string option -> unit)
    }

val newStartAddText: ssi: StartStopInsert -> StartAddText

val newChatInput: b: Gtk.Builder -> Gtk.TextView

val newChatDisplay: b: Gtk.Builder -> Gtk.TextView

val newDisplayInput: b: Gtk.Builder -> DisplayInput

val displayProviderModel:
  b: Gtk.Builder -> p: Types.Provider -> m: Types.Model -> unit

val newShowCommands: b: Gtk.Builder -> ShowCommands

val confChatInputEvent:
  chatInput: Gtk.TextView -> f: (Gdk.EventKey -> unit) -> unit

val confSend: b: Gtk.Builder -> ClickSubscriber


module ReadAnswer

val readAnswer:
  sa: Controls.StartAddText -> read: (unit -> Async<string option>) -> unit


module SelectProviderModel

val getActiveAnswerer: c: Types.Config -> Types.Answerer

val getActiveModels: c: Types.Config -> Types.Model list

val setProviderModel:
  c: Types.Config ->
    spm: Controls.SetProviderModel -> pm: Types.ProviderModel -> Types.Config


module MakeQuestion

type QuestionAnswerer =
    {
      getQuestion: (unit -> Types.Question)
      answerer: Types.Answerer
    }

type ReadStop =
    {
      stop: (unit -> Async<string option>)
      read: (unit -> Async<string option>)
    }

val makeQuestion: qa: QuestionAnswerer -> ReadStop


module HandleCommands

type Commands =
    {
      showCommands: Controls.ShowCommands
      makeQuestion: (unit -> unit)
    }

val handleKeyPress: commands: Commands -> e: Gdk.EventKey -> unit


module ChatWindow

type ChatWindow =
    inherit Gtk.Window
    
    new: baseBuilder: nativeint -> ChatWindow

val newChatWindow: providers: Map<string,Types.ProviderAnswerers> -> ChatWindow


module Program

val getenv: s: string -> string option

val initProviders: unit -> Map<string,Types.ProviderAnswerers>

val main: string array -> int

