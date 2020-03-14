//-----------------------------------------------------------------------
// <copyright file="Source.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open Akka.Streams
open Akka.Streams.Dsl

module FlowWithContext =
    
    let inline asFlow (flow: FlowWithContext<_,_,_,_,_>) = flow.AsFlow()


module SourceWithContext =
    
    let inline asSource (source: SourceWithContext<_,_,_>) = source.AsSource()
    
    

