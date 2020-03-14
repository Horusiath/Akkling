//-----------------------------------------------------------------------
// <copyright file="Tcp.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
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
        tcp.Bind(host, port).MapMaterializedValue(Func<_,_> Async.AwaitTask)
        
    /// Asynchronously creates a TCP server binding for a given host and port.
    let inline bindAndHandle (mat: #IMaterializer) (host: string) (port: int) (handler: Flow<ByteString,ByteString,unit>) (tcp: TcpExt) =
        tcp.BindAndHandle(handler.MapMaterializedValue(fun _ -> NotUsed.Instance), mat, host, port) |> Async.AwaitTask

    /// Creates an async client TCP connection.
    let inline outgoing (host: string) (port: int) (tcp: TcpExt): Flow<ByteString,ByteString, Async<Tcp.OutgoingConnection>> =
        tcp.OutgoingConnection(host, port).MapMaterializedValue(Func<_,_> Async.AwaitTask)
        