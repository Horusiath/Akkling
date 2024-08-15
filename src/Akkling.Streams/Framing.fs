//-----------------------------------------------------------------------
// <copyright file="Framing.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akka.Streams
open Akka.Streams.Dsl
open Akka.IO
open Akka.Streams.Dsl

[<RequireQualifiedAccess>]
module Framing =

    /// Creates a Flow that handles decoding a stream of unstructured byte chunks into a stream of frames where the
    /// incoming chunk stream uses a specific byte-sequence to mark frame boundaries.
    /// The decoded frames will not include the separator sequence.
    let inline delimiter
        (allowTruncate: bool)
        (maxLength: int)
        (delim: ByteString)
        : Flow<ByteString, ByteString, unit> =
        Akka.Streams.Dsl.Framing
            .Delimiter(delim, maxLength, allowTruncate)
            .MapMaterializedValue(Func<_, _>(ignore))

    /// Creates a Flow that decodes an incoming stream of unstructured byte chunks into a stream of frames, assuming that
    /// incoming frames have a field that encodes their length.
    /// If the input stream finishes before the last frame has been fully decoded this Flow will fail the stream reporting
    /// a truncated frame.
    let inline lengthFieldWithOffset
        (offset: int)
        (order: ByteOrder)
        (fieldLength: int)
        (maxLength: int)
        : Flow<ByteString, ByteString, unit> =
        Akka.Streams.Dsl.Framing
            .LengthField(fieldLength, maxLength, offset, order)
            .MapMaterializedValue(Func<_, _>(ignore))

    /// Creates a Flow that decodes an incoming stream of unstructured byte chunks into a stream of frames, assuming that
    /// incoming frames have a field that encodes their length.
    /// If the input stream finishes before the last frame has been fully decoded this Flow will fail the stream reporting
    /// a truncated frame.
    let lengthField = lengthFieldWithOffset 0

    let inline simpleEncoder (maxLength: int) : Flow<ByteString, ByteString, unit> =
        Akka.Streams.Dsl.Framing
            .SimpleFramingProtocolEncoder(maxLength)
            .MapMaterializedValue(Func<_, _>(ignore))

    let inline simpleDecoder (maxLength: int) : Flow<ByteString, ByteString, unit> =
        Akka.Streams.Dsl.Framing
            .SimpleFramingProtocolDecoder(maxLength)
            .MapMaterializedValue(Func<_, _>(ignore))

    /// Returns a BidiFlow that implements a simple framing protocol. This is a convenience wrapper over <see cref="LengthField"/>
    /// and simply attaches a length field header of four bytes (using big endian encoding) to outgoing messages, and decodes
    /// such messages in the inbound direction. The decoded messages do not contain the header.
    /// This BidiFlow is useful if a simple message framing protocol is needed (for example when TCP is used to send
    /// individual messages) but no compatibility with existing protocols is necessary.
    /// The encoded frames have the layout
    /// {{{
    ///     [4 bytes length field, Big Endian][User Payload]
    /// }}}
    let inline simpleProtocol (maxLength: int) : BidiFlow<ByteString, ByteString, ByteString, ByteString, unit> =
        BidiFlow.FromFlowsMat(
            Akka.Streams.Dsl.Framing.SimpleFramingProtocolEncoder(maxLength),
            Akka.Streams.Dsl.Framing.SimpleFramingProtocolDecoder(maxLength),
            Func<_, _, unit>(fun _ _ -> ())
        )

    /// Returns a Flow that implements a "brace counting" based framing stage for emitting valid JSON chunks.
    /// It scans the incoming data stream for valid JSON objects and returns chunks of ByteStrings containing only those valid chunks.
    let inline json (maxObjectLength: int) : Flow<ByteString, ByteString, unit> =
        Akka.Streams.Dsl.JsonFraming
            .ObjectScanner(maxObjectLength)
            .MapMaterializedValue(Func<_, _>(ignore))
