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
        
let private jobjectType = Type.GetType("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json") 
let private jsonSerlizerType = Type.GetType("Newtonsoft.Json.JsonSerializer, Newtonsoft.Json")
let private toObjectMethod = jobjectType.GetMethod("ToObject", [|typeof<System.Type>; jsonSerlizerType|])

let tryDeserializeJObject jsonSerializer o : 'Message option =
    let t = typeof<'Message>
    if o <> null && o.GetType().Equals jobjectType 
    then 
        try
            let res = toObjectMethod.Invoke(o, [|t; jsonSerializer|]) 
            Some (res :?> 'Message)
        with
        | _ -> None // type conversion failed (passed JSON is not of expected type)
    else None

// used for top level serialization
type ExprSerializer(system) = 
    inherit Serializer(system)
    let fsp = FsPickler.CreateBinary()
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

