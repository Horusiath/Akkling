//-----------------------------------------------------------------------
// <copyright file="Serialization.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Serialization

open Akka.Actor
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation
open Nessos.FsPickler
open Akka.Serialization

let internal serializeToBinary (fsp:BinarySerializer) o : byte array = fsp.Pickle o

let internal deserializeFromBinary (fsp:BinarySerializer) (bytes: byte array) : 'Deserialized = fsp.UnPickle bytes
        

// used for top level serialization
type ExprSerializer(system) = 
    inherit Serializer(system)
    let fsp = FsPickler.CreateBinarySerializer()
    override __.Identifier = 9
    override __.IncludeManifest = true
    override __.ToBinary(o) = serializeToBinary fsp (o :?> Expr)
    override __.FromBinary(bytes, _) =
        let deserialized: Expr = deserializeFromBinary fsp bytes
        upcast deserialized

let internal exprSerializationSupport (system : ActorSystem) = 
    let serializer = ExprSerializer(system :?> ExtendedActorSystem)
    system.Serialization.AddSerializer(serializer)
    system.Serialization.AddSerializationMap(typeof<Expr>, serializer)

