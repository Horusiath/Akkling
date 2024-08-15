namespace Akkling.Hocon

[<AutoOpen>]
module Inbox =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type InboxBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<CustomOperation("default_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DefaultTimeout(state: string list, value: 'Duration) =
            durationField "default-timeout" value :: state

        member inline this.default_timeout(value: 'Duration) =
            this.DefaultTimeout([], value) |> this.Run

        [<CustomOperation("inbox_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.InboxSize(state: string list, value: int) =
            positiveField "inbox-size" value :: state

        member this.inbox_size value = this.InboxSize([], value) |> this.Run

    let inbox =
        { new InboxBuilder<Akka.Actor.Field>() with
            member _.Run(state: string list) =
                objExpr "inbox" 3 state |> Akka.Actor.Field }
