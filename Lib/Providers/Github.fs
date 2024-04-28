module R0b0t.Provider.Github

open System
open System.IO
open System.Net
open System.Text.Json
open System.Threading.Channels

open FsHttp
open FSharp.Control

open Types

type GithubAuth = { oauthToken: string; token: string }

type FilterOffsets =
  { check_offset: int
    end_offset: int
    start_offset: int }

type FilterResult = { filtered: bool; severity: string }

type FilterResults =
  { hate: FilterResult
    self_harm: FilterResult
    sexual: FilterResult
    violence: FilterResult }

type Delta =
  { content: string option
    role: string option }

type Choice =
  { content_filter_offsets: FilterOffsets
    content_filter_results: FilterResults
    delta: Delta
    finish_reason: string option
    index: int }

type CompletionChunk =
  { choices: Choice list
    created: int64
    id: string }

type AuthResp =
  { annotations_enabled: bool
    chat_enabled: bool
    chat_jetbrains_enabled: bool
    code_quote_enabled: bool
    copilot_ide_agent_chat_gpt4_small_prompt: bool
    copilotignore_enabled: bool
    expires_at: int64
    intellij_editor_fetcher: bool
    prompt_8k: bool
    public_suggestions: string
    refresh_in: int
    sku: string
    snippy_load_test_enabled: bool
    telemetry: string
    token: string
    tracking_id: string
    vsc_panel_v2: bool }

let getAuthorizationFromKey (key: string) =
  http {
    GET "https://api.github.com/copilot_internal/v2/token"
    Authorization $"token {key}"
    UserAgent "Go-http-client/1.1"
  }
  |> Request.send
  |> Response.expectHttpStatusCode HttpStatusCode.OK
  |> Result.map (fun r -> r.DeserializeJson<AuthResp>())

let genHex (n: int) =
  let random = Random()
  let hexChars = "0123456789abcdef".ToCharArray()
  let sb = System.Text.StringBuilder(n)

  for _ in 1..n do
    let index = random.Next(hexChars.Length)
    sb.Append(hexChars.[index]) |> ignore

  sb.ToString()

let asyncBody (r: Response) =
  asyncSeq {
    let reader = new StreamReader(r.content.ReadAsStream())

    while not reader.EndOfStream do
      let! line = reader.ReadLineAsync() |> Async.AwaitTask

      match line.Split("data: ") with
      | [| _; "[DONE]" |] -> ()
      | [| _; x |] ->
        match (JsonSerializer.Deserialize<CompletionChunk> x).choices with
        | [ { delta = { content = Some x } } ] -> yield x
        | _ -> ()
      | _ -> ()
  }

let sendChatReq (token: string) (userMsg: string) =
  http {
    header "Authorization" $"Bearer {token}"
    header "X-Request-Id" $"{genHex 8}-{genHex 4}-{genHex 4}-{genHex 4}-{genHex 12}"
    header "Vscode-Sessionid" $"{genHex 8}-{genHex 4}-{genHex 4}-{genHex 4}-{genHex 25}"
    header "Vscode-Machineid" (genHex 64)
    header "Editor-Version" "vscode/1.83.1"
    header "Editor-Plugin-Version" "copilot-chat/0.11.1"
    header "Openai-Organization" "github-copilot"
    header "Openai-Intent" "conversation-panel"
    header "User-Agent" "GitHubCopilotChat/0.11.1"
    header "Accept" "*/*"
    header "Accept-Encoding" "gzip,deflate,br"
    header "Connection" "close"
    POST "https://api.githubcopilot.com/chat/completions"
    body

    jsonSerialize
      {| messages =
          [ {| role = "system"
               content =
                "You are ChatGPT, a large language model trained by OpenAI.
                      Knowledge cutoff: 2021-09
                      Current model: gpt-4" |}
            {| role = "user"; content = userMsg |} ]
         model = "gpt-4"
         temperature = 0.5
         top_p = 1
         n = 1
         stream = true |}
  }
  |> Request.send

[<Literal>]
let retriesLimit = 10

let chatCompletion (auth: GithubAuth) (userMsg: string) =
  let rec retryLoop (token: string) (retries: int) =
    sendChatReq token userMsg
    |> Response.expectHttpStatusCodes [ HttpStatusCode.OK; HttpStatusCode.Unauthorized; HttpStatusCode.BadRequest ]
    |> function
      | Ok r when r.statusCode = HttpStatusCode.OK -> { auth with token = token }, asyncBody r
      | Ok r when r.statusCode = HttpStatusCode.Unauthorized && retries = retriesLimit ->
        auth,
        [ $"{retriesLimit} Github authentication attemps failed, stopping now" ]
        |> AsyncSeq.ofSeq
      | Ok r when
        r.statusCode = HttpStatusCode.Unauthorized
        || r.statusCode = HttpStatusCode.BadRequest
        ->
        //printfn $"body {r.content.ReadAsStringAsync().Result}"
        match getAuthorizationFromKey auth.oauthToken with
        | Ok r -> retryLoop r.token (retries + 1)
        | Error e -> auth, [ e.ToString() ] |> AsyncSeq.ofSeq
      | Ok r ->
        auth,
        asyncSeq {
          let reader = new StreamReader(r.content.ReadAsStream())

          while not reader.EndOfStream do
            let! line = reader.ReadLineAsync() |> Async.AwaitTask
            yield line
        }
      | Error e -> auth, [ e.ToString() ] |> AsyncSeq.ofSeq

  retryLoop auth.token 0

let answer (auth: GithubAuth) (question: string, consumer: Channel<string option>) =
  let token, xs = chatCompletion auth question

  token,
  async {
    let! answer =
      xs
      |> AsyncSeq.foldAsync
        (fun text s ->
          async {
            do! consumer.Writer.WriteAsync(Some s) |> _.AsTask() |> Async.AwaitTask
            return $"{text}{s}"
          })
        ""

    do! consumer.Writer.WriteAsync None |> _.AsTask() |> Async.AwaitTask
    return answer
  }

// TODO make more flexible interface so this mutable can go away
// ouath token looks like "ghu_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" and has length 40
let mutable auth = { oauthToken = ""; token = "bla" }

let ask (key: string) (_: string) (question: string, c: Channel<string option>) =
  let nauth, s = answer { oauthToken = key; token = "" } (question, c)

  async {
    let! _ = s
    return ()
  }
  |> Async.Start
  //printfn $"s = {s |> Async.RunSynchronously}"
  //printfn $"nauth = {nauth}"
  auth <- nauth

[<Literal>]
let environmentVar = "github_key"

let getProvider (key: string) =
  { name = "GitHub"
    models = [ "Copilot" ]
    implementation = ask key }
