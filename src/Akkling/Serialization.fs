//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

module Linq = 
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq
    
    let (|Lambda|_|) (e : Expression) = 
        match e with
        | :? LambdaExpression as l -> Some(l.Parameters, l.Body)
        | _ -> None
    
    let (|Call|_|) (e : Expression) = 
        match e with
        | :? MethodCallExpression as c -> Some(c.Object, c.Method, c.Arguments)
        | _ -> None
    
    let (|Method|) (e : System.Reflection.MethodInfo) = e.Name
    
    let (|Invoke|_|) = 
        function 
        | Call(o, Method("Invoke"), _) -> Some o
        | _ -> None
    
    let (|Ar|) (p : System.Collections.ObjectModel.ReadOnlyCollection<Expression>) = Array.ofSeq p
    
    let toExpression<'Actor> (f : System.Linq.Expressions.Expression) = 
        match f with
        | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) | Call(null, Method "ToFSharpFunc", 
                                                                                             Ar [| Lambda(_, p) |]) -> 
            Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<'Actor>>
        | _ -> failwith "Doesn't match"
    
    type Expression = 
        static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunActor<'Message, 'v>>>) = 
            toExpression<FunActor<'Message, 'v>> f
        static member ToExpression<'Actor>(f : Quotations.Expr<unit -> 'Actor>) = 
            toExpression<'Actor> (QuotationEvaluator.ToLinqExpression f)

module Serialization = 
    open Nessos.FsPickler
    open Akka.Serialization
    
    let internal serializeToBinary (fsp : BinarySerializer) (o: 'TSerialized) = 
        use stream = new System.IO.MemoryStream()
        fsp.Serialize(stream, o)
        stream.ToArray()
    
    let internal deserializeFromBinary (fsp : BinarySerializer) (bytes : byte array): 'TDeserialized = 
        use stream = new System.IO.MemoryStream(bytes)
        fsp.Deserialize(stream)
    
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

