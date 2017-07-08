//-----------------------------------------------------------------------
// <copyright file="IO.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

module IO =

    open Akka.IO
    open System.Net

    /// <summary>
    /// Gets TCP manager for current actor.
    /// </summary>
    let Tcp(context: Actor<'Message>) : IActorRef<Akka.IO.Tcp.Command> =
        typed (Akka.IO.Tcp.Manager(context.System))
        
    /// <summary>
    /// Gets UDP manager for current actor.
    /// </summary>
    let Udp(context: Actor<'Message>) : IActorRef<Akka.IO.Udp.Command> =
        typed (Akka.IO.Udp.Manager(context.System))

    type TcpMessage = Akka.IO.TcpMessage 

    module Tcp =

        let (|Received|_|) (msg:obj) : ByteString option =
            match msg with
            | :? Tcp.Received as r -> Some (r.Data)
            | _ -> None
            
        let (|Connected|_|) (msg:obj) : (EndPoint * EndPoint) option =
            match msg with
            | :? Tcp.Connected as c -> Some (c.RemoteAddress, c.LocalAddress)
            | _ -> None

        let (|CommandFailed|_|) (msg:obj) : #Tcp.Command option =
            match msg with
            | :? Tcp.CommandFailed as c -> Some (c.Cmd :?> #Tcp.Command)
            | _ -> None

        let (|WritingResumed|_|) (msg:obj) =
            match msg with
            | :? Tcp.WritingResumed -> Some ()
            | _ -> None

        let (|Bound|_|) (msg:obj) =
            match msg with
            | :? Tcp.Bound as bound -> Some bound.LocalAddress
            | _ -> None

        let (|Unbound|_|) (msg:obj) =
            match msg with
            | :? Tcp.Unbound -> Some()
            | _ -> None

        let (|ConnectionClosed|_|) (msg:obj) =
            match msg with
            | :? Tcp.ConnectionClosed as closed -> Some closed
            | _ -> None

        let (|Closed|Aborted|ConfirmedClosed|PeerClosed|ErrorClosed|) (msg:Tcp.ConnectionClosed) =
            match msg with
            | :? Tcp.Closed -> Closed
            | :? Tcp.Aborted -> Aborted
            | :? Tcp.ConfirmedClosed -> ConfirmedClosed
            | :? Tcp.PeerClosed -> PeerClosed
            | :? Tcp.ErrorClosed -> ErrorClosed
            
    module Udp =

        let inline Bind(ref: IActorRef<'t>, localAddress: EndPoint) = 
            Akka.IO.Udp.Bind(ref, localAddress) :> Udp.Command
            
        let (|Received|_|) (msg:obj) : ByteString option =
            match msg with
            | :? Udp.Received as r -> Some (r.Data)
            | _ -> None

        let (|CommandFailed|_|) (msg:obj) : 'C option =
            match msg with
            | :? Udp.CommandFailed as c -> 
                if c.Cmd :? 'C
                then Some (c.Cmd :?> 'C)
                else None
            | _ -> None
