//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.Api

open Akkling
open Akka.Actor
open System
open Xunit

[<Fact>]
let ``configuration loader should load data from app.config``() = 
    let config = Configuration.load()
    config.HasPath "akka.test.value" |> equals true
    config.GetInt "akka.test.value" |> equals 10

[<Fact>]
let ``can serialize expression decider``() = 
    let decider = ExprDecider <@ fun e -> Directive.Resume @>
    use sys = System.create "system" (Configuration.defaultConfig())
    let serializer = sys.Serialization.FindSerializerFor decider
    let bytes = serializer.ToBinary decider
    let des = serializer.FromBinary(bytes, typeof<ExprDecider>) :?> IDecider
    des.Decide(Exception()) |> equals (Directive.Resume)

type TraceableMessage<'a> = 
    { Trace : TraceMetadata
      Message : 'a }

and TraceMetadata = 
    | NoTrace
    | Trace of TraceId : uint64 * SpanId : uint64 * ParentSpanId : uint64 option

type Availability = 
    | NotFound
    | UnableToDetermineAvailability of Reason : string
    | Availability of AvailabilityAtTimeHorizon list

and AvailabilityAtTimeHorizon = 
    { AvailableAsOf : DateTime
      Quantity : decimal }

type ItemAvailabilityMessage = 
    | QueryItemAvailability of Handle : string * Options : AvailabilityOptions
    | Reconfigure

and AvailabilityOptions = 
    { SkipCache : bool }
    static member Defaults = { SkipCache = false }

type ItemAvailabilityReply = 
    | AvailabilityReply of Handle : string * Availability : Availability

[<Fact>]
let ``can serialize discriminated unions``() = 
    let x = 
        { Trace = Trace(10UL, 1000UL, Some 11000UL)
          Message = 
              Availability([ { AvailableAsOf = DateTime.Now
                               Quantity = 10M } ]) }
    
    use sys = System.create "system" (Configuration.defaultConfig())
    let serializer = sys.Serialization.FindSerializerFor x
    let bytes = serializer.ToBinary x
    let des = serializer.FromBinary(bytes, typeof<TraceableMessage<Availability>>) :?> TraceableMessage<Availability>
    des |> equals x

[<Fact>]
let ``can serialize typed actor ref``() = 
    use sys = System.create "system" (Configuration.defaultConfig())
    let echo = spawn sys "echo" <| actorOf2 (fun mailbox msg -> mailbox.Sender() <! msg)
    let serializer = sys.Serialization.FindSerializerFor echo
    let bytes = serializer.ToBinary echo
    let des = serializer.FromBinary(bytes, echo.GetType()) :?> IActorRef<string>
    des |> equals echo
