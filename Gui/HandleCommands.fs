module HandleCommands

type Commands =
  { showCommands: Controls.ShowCommands
    makeQuestion: unit -> unit }

let handleKeyPress (commands: Commands) (e: Gdk.EventKey) =
  match e.Key with
  | Gdk.Key.Return when e.State.HasFlag Gdk.ModifierType.ControlMask -> commands.makeQuestion ()
  | Gdk.Key.p when e.State.HasFlag Gdk.ModifierType.ControlMask -> commands.showCommands ()
  | _ -> ()
