namespace Akkling.Hocon

[<AutoOpen>]
module Gremlin =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type GremlinBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Enable debug logging of the failure injector transport adapter
        [<CustomOperation("debug"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Debug(state: string list, value: bool) = switchField "debug" value :: state

        /// Enable debug logging of the failure injector transport adapter
        member this.debug value = this.Debug([], value) |> this.Run

    let gremlin =
        { new GremlinBuilder<Akka.Remote.Field>() with
            member _.Run(state: string list) =
                objExpr "gremlin" 3 state |> Akka.Remote.Field }
