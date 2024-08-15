namespace Akkling.Hocon

[<AutoOpen>]
module IO =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type IOBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "io" indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Actor.Dispatcher.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Tcp.Field>) = [ (x 3).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Udp.Field>) = [ (x 3).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.IO.Field) = [ x.ToString() ]

        // Pathing

        member _.tcp =
            { new TcpBuilder<Akka.Field>(Akka.Field, "io.tcp") with
                member _.Run(state: SharedInternal.State<Tcp.Target>) = fun _ -> pathObjExpr "tcp" state.Fields }

        member _.udp =
            { new UdpBuilder<Akka.Field>(Akka.Field, "io.udp") with
                member _.Run(state: SharedInternal.State<Udp.Target>) = fun _ -> pathObjExpr "udp" state.Fields }

        member _.udp_connected =
            { new UdpBuilder<Akka.Field>(Akka.Field, "io.udp-connected") with
                member _.Run(state: SharedInternal.State<Udp.Target>) =
                    fun _ -> pathObjExpr "udp-connected" state.Fields }

        member _.dns =
            { new DnsBuilder<Akka.Field>(Akka.Field, "io.dns", indent) with
                member _.Run(state: string list) = pathObjExpr "dns" state }

    let io =
        { new IOBuilder<Akka.Field>() with
            member _.Run(state: string list) = objExpr "io" 2 state |> Akka.Field }
