module StreamDeps.StopInsert

open Gtk
open Stream.Types

type AdjustWord =
  { chatDisplay: TextView
    adjustment: Adjustment }

let insertWord (b: Builder) : string -> unit =
  let adj = b.GetObject "text_adjustment" :?> Adjustment
  let chatDisplay = b.GetObject "chat_display" :?> TextView

  let f w =
    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor w
    adj.Value <- adj.Upper

  f

let newStopInsert (builder: Builder) =
  let answerSpinner = builder.GetObject "answer_spinner" :?> Spinner

  { stop = answerSpinner.Stop
    insertWord = insertWord builder }
