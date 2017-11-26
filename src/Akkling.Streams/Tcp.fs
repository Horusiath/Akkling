//-----------------------------------------------------------------------
// <copyright file="Tcp.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akkling
open Akka
open Akka.IO
open Akka.Streams
open Akka.Streams.Dsl

[<RequireQualifiedAccess>]
module Tcp =

    /// Asynchronously creates a TCP server binding for a given host and port.
    let inline bind (host: string) (port: int) (tcp: TcpExt) =
        tcp.Bind(host, port).MapMaterializedValue(fun t -> Async.AwaitTask t)
        
    /// Asynchronously creates a TCP server binding for a given host and port.
    let inline bindAndHandle (mat: #IMaterializer) (host: string) (port: int) (handler: Flow<ByteString,ByteString,unit>) (tcp: TcpExt) =
        tcp.BindAndHandle(handler.MapMaterializedValue(fun () -> NotUsed.Instance), mat, host, port) |> Async.AwaitTask

    /// Creates an async client TCP connection.
    let inline outgoing (host: string) (port: int) (tcp: TcpExt): Flow<ByteString,ByteString, Async<Tcp.OutgoingConnection>> =
        tcp.OutgoingConnection(host, port).MapMaterializedValue(fun t -> Async.AwaitTask t)

[<RequireQualifiedAccess>]
module Framing =

    /// Create a flow, which turns a stream of unstructured byte chunks into structured frames,
    /// where delimeter is used to mark frame boundaries. If `allowTruncate` is true, then when
    /// stream ends without delimeter, a truncated frame will be emitted before finishing the stream.
    let inline delimiter (allowTruncate: bool) (max: int) (delim: ByteString): Flow<ByteString, ByteString, unit> =
        Framing.Delimiter(delim, max, allowTruncate).MapMaterializedValue(fun _ -> ())
        
    /// Create a flow, which turns a stream of unstructured byte chunks into structured frames,
    /// assuming that the first 4B prefix determines the length of a frame.
    let inline lengthField (offset: int) (max: int) (len: int): Flow<ByteString, ByteString, unit> =
        Framing.LengthField(len, max, offset).MapMaterializedValue(fun _ -> ())

    let inline simpleDecoder (max: int): Flow<ByteString, ByteString, unit> =
        Framing.SimpleFramingProtocolDecoder(max).MapMaterializedValue(fun _ -> ())
        
    let inline simpleEncoder (max: int): Flow<ByteString, ByteString, unit> =
        Framing.SimpleFramingProtocolEncoder(max).MapMaterializedValue(fun _ -> ())