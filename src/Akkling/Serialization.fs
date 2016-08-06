//-----------------------------------------------------------------------
// <copyright file="Serialization.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
namespace Akkling.Serialization

open Akka.Actor
open Akka.Util
open System
open System.IO
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation
        
open MBrace.FsPickler

// used for top level serialization
type ExprSerializer(system) = 
    inherit Akka.Serialization.Serializer(system)
    let fsp = FsPickler.CreateBinarySerializer()
    override __.Identifier = 9
    override __.IncludeManifest = true
    override __.ToBinary(o) = fsp.Pickle (o :?> Expr)
    override __.FromBinary(bytes, _) =
        let deserialized: Expr = fsp.UnPickle bytes
        upcast deserialized
