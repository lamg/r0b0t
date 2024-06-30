module GUI.InputOutput

open System
open Gdk
open Gtk
open GetProviderImpl
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png.Chunks
open Stream.Types

type InputOutput =
  { getPrompt: unit -> Prompt
    keyRelease: (KeyReleaseEventArgs -> unit) -> unit
    insertWord: string -> unit
    insertImage: PngData -> unit }

[<Literal>]
let promptPngMetadataKey = "prompt"

[<Literal>]
let revisedPromptPngMetadataKey = "revised_prompt"

let saveImage (d: PngData) =
  use image = Image.Load d.image
  let pngMeta = image.Metadata.GetPngMetadata()
  pngMeta.TextData.Add(PngTextData(promptPngMetadataKey, d.prompt, "en", promptPngMetadataKey))
  pngMeta.TextData.Add(PngTextData(revisedPromptPngMetadataKey, d.revisedPrompt, "en", revisedPromptPngMetadataKey))
  let now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss")
  let outputPath = $"img_{now}.png"
  image.Save(outputPath)

let insertImage (insertWord: string -> unit) (chatDisplay: TextView) (d: PngData) =

  saveImage d
  let p = new PixbufLoader(d.image)
  let height = chatDisplay.AllocatedHeight / 2
  let width = chatDisplay.AllocatedWidth / 2
  let np = p.Pixbuf.ScaleSimple(height, width, InterpType.Bilinear)
  let buff = chatDisplay.Buffer

  GLib.Idle.Add(fun _ ->
    buff.InsertPixbuf(ref buff.EndIter, np)
    insertWord d.revisedPrompt
    insertWord "\n\n"
    false)
  |> ignore

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
