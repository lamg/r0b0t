module MakeQuestion

open System.Threading.Channels

open Types

type QuestionFlow =
  { getQuestion: Controls.GetQuestion
    getImplementation: SelectProviderModel.GetActiveImplementation
    readAnswer: ReadAnswer.ReadAnswer }

let makeQuestion (flow: QuestionFlow) =
  let answer = Channel.CreateUnbounded<string option>()
  let imp = flow.getImplementation ()
  let question = flow.getQuestion ()
  imp (question, answer)
  flow.readAnswer answer

let makeQuestion2 (gq: Controls.GetQuestion) =
  let mb = MailboxProcessor.Start R0b0t.Provider.Dummy.provider2
  let question = gq ()

  Question question |> mb.Post

  let pr x () =
    mb.PostAndTryAsyncReply(x, timeout = 10000)

  { stop = pr Stop
    read = pr AnswerSegment }
  : ReadAnswer.ReadStop
