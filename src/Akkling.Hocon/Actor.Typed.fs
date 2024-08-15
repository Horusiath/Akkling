namespace Akkling.Hocon

[<AutoOpen>]
module Typed =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type TypedBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<CustomOperation("timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Timeout(state: string list, value: 'Duration) = durationField "timeout" value :: state

        member inline this.timeout(value: 'Duration) = this.Timeout([], value) |> this.Run

    /// THIS DOES NOT APPLY TO .NET
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    let typed' =
        { new TypedBuilder<Akka.Actor.Field>() with
            member _.Run(state: string list) =
                objExpr "typed" 3 state |> Akka.Actor.Field }
