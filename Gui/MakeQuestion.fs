module MakeQuestion

open System.Threading.Channels

type QuestionFlow =
  { getQuestion: GetQuestion.GetQuestion
    getImplementation: SelectProviderModel.GetActiveImplementation
    readAnswer: ReadAnswer.ReadAnswer }

let makeQuestion (flow: QuestionFlow) =
  let answer = Channel.CreateUnbounded<string option>()
  let imp = flow.getImplementation ()
  let question = flow.getQuestion ()
  imp (question, answer)
  flow.readAnswer answer
