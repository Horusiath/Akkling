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

    /// Helper module with active patterns for TCP-related commands and events.
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
            
    /// Helper module with active patterns for UDP-related commands and events.
    module Udp =

        let inline Bind(ref: IActorRef<'t>, localAddress: EndPoint) = 
            Akka.IO.Udp.Bind(untyped ref, localAddress) :> Udp.Command
            
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

    [<RequireQualifiedAccess>]
    module ByteString =

        open System.IO

        /// Gets a total number of bytes stored inside this byte string.
        let inline length (b: ByteString) = b.Count

        /// Determines if current byte string is empty.
        let inline isEmpty (b: ByteString) = b.IsEmpty

        /// Determines if current byte string has compact representation.
        /// Compact byte strings represent bytes stored inside single, continuous
        /// block of memory.
        let inline isCompact (b: ByteString) = b.IsCompact

        /// Compacts current byte string, potentially copying its content underneat
        /// into new byte array.
        let inline compact (b: ByteString) = b.Compact()
        
        /// Slices current byte string, creating a new one which contains a specified 
        /// range of data from the original. This is non-copying operation.
        let inline slice (index: int) (length: int) (b: ByteString) = b.Slice(index, length)

        /// Returns an index of the first occurence of provided byte starting 
        /// from the provided index.
        let inline indexOf (from: int) (byte: byte) (b: ByteString) = b.IndexOf(byte, from)

        /// Checks if a subsequence determined by the other byte string is 
        /// can be found in current one, starting from provided index.
        let inline contains (from: int) (other: ByteString) (b: ByteString) = b.HasSubstring(other, from)

        /// Copies content of a current byte string into a single byte array.
        let inline toArray (b: ByteString) = b.ToArray()

        /// Appends second parameter byte string at the tail
        /// of a first one, creating a new byte string in result.
        /// Contents of byte strings are NOT copied.
        let inline append (a: ByteString) (b: ByteString) = a.Concat b
        
        /// Copies content of the current byte string to a provided 
        /// writeable stream.
        let inline writeTo (stream: Stream) (b: ByteString) = b.WriteTo stream
        
        /// Copies content of the current byte string to a provided 
        /// writeable stream.
        let inline asyncWriteTo (stream: Stream) (b: ByteString) = b.WriteToAsync stream |> Async.AwaitTask