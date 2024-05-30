module GUI.InputOutput

open System
open Gdk
open Gtk

open GetProviderImpl

type InputOutput =
  { getPrompt: unit -> Prompt
    keyRelease: (KeyReleaseEventArgs -> unit) -> unit
    insertWord: string -> unit
    insertImage: string -> unit }

let saveImage (bs: byte array) =
  let now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss")
  IO.File.WriteAllBytes($"img_{now}.png", bs)

let insertImage (insertWord: string -> unit) (chatDisplay: TextView) (content: string) =
  let bs = Convert.FromBase64String content
  saveImage bs
  let p = new PixbufLoader(bs)
  let height = chatDisplay.AllocatedHeight / 2
  let width = chatDisplay.AllocatedWidth / 2
  let np = p.Pixbuf.ScaleSimple(height, width, InterpType.Bilinear)
  let buff = chatDisplay.Buffer

  GLib.Idle.Add(fun _ ->
    buff.InsertPixbuf(ref buff.EndIter, np)
    false)
  |> ignore

  insertWord ""


let getPrompt (chatInput: TextView, chatDisplay: TextView) =
  fun () ->
    let question = chatInput.Buffer.Text

    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor $"🧑: {question}\n🤖: "
    question

let insertWord (chatDisplay: TextView, adjustment: Adjustment) (w: string) =
  GLib.Idle.Add(fun _ ->
    chatDisplay.Buffer.PlaceCursor chatDisplay.Buffer.EndIter
    chatDisplay.Buffer.InsertAtCursor w
    adjustment.Value <- adjustment.Upper
    false)
  |> ignore



let newInputOutput (w: Window) (b: Builder) : InputOutput =
  let chatDisplay = b.GetObject "chat_display" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatDisplay.StyleContext.AddProvider(provider, 0u)

  let chatInput = b.GetObject "chat_input" :?> TextView
  let mutable provider = new CssProvider()
  provider.LoadFromData("textview { font-size: 18pt;}") |> ignore
  chatInput.StyleContext.AddProvider(provider, 0u)
  chatInput.GrabFocus()

  let adjustment = b.GetObject "text_adjustment" :?> Adjustment
  let insertWord = insertWord (chatDisplay, adjustment)

  { getPrompt = getPrompt (chatInput, chatDisplay)
    keyRelease = w.KeyReleaseEvent.Add
    insertWord = insertWord
    insertImage = insertImage insertWord chatDisplay }
