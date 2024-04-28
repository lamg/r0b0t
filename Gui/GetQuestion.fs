module GetQuestion

type GetQuestion = unit -> string

let newGetQuestion (di: Controls.DisplayInput) : GetQuestion =
  let f () =
    let question = di.chatInput.Buffer.Text

    di.chatDisplay.Buffer.PlaceCursor di.chatDisplay.Buffer.EndIter
    di.chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
    question

  f
