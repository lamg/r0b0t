module HandleCommands

type Commands =
  { showCommands: Controls.ShowCommands
    makeQuestion: unit -> unit }

let handleKeyPress (commands: Commands) (e: Gdk.EventKey) =
  match e.Key, e.State with
  | Gdk.Key.Return, Gdk.ModifierType.ControlMask -> commands.makeQuestion ()
  | Gdk.Key.p, Gdk.ModifierType.ControlMask -> commands.showCommands ()
  | _ -> ()
