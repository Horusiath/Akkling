//-----------------------------------------------------------------------
// <copyright file="Configuration.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.Configuration

open Akkling
open System
open Xunit
open Akka.Configuration

type MyEnum =
    | A = 1
    | B = 2

let config = Configuration.parse """
outer {
    inner {
        inner2 {
            my-prop = "hello"
        }
        inner3 {
            my-other-prop = "world"
        }
    }
    multi-part-property = 999999999999
    an-integer = 1
    some-strange-float = 2.5
    a-boolean = on
    a-time = 30s
    list-of-strings = [ "hello", "hocon" ]
    option-type = b
}
"""

[<Fact>]
let ``Dynamic config operation must return Config`` () = 
    let c1 = config?outer 
    let (value: Config) = c1?Inner?Inner2
    equals typeof<Config> <| value.GetType()
    equals true <| value.HasPath "my-prop"

[<Fact>]
let ``Dynamic config operation must return string`` () = 
    let c1 = config?outer 
    let (value: string) = c1?Inner?Inner2?MyProp
    equals "hello" value

[<Fact>]
let ``Dynamic config operation must return int32`` () = 
    let c1 = config?outer 
    let (value: int) = c1?AnInteger
    equals 1 value

[<Fact>]
let ``Dynamic config operation must return int64`` () = 
    let c1 = config?outer 
    let (value: int64) = c1?MultiPartProperty
    equals 999999999999L value

[<Fact>]
let ``Dynamic config operation must return bool`` () = 
    let c1 = config?outer 
    let (value: bool) = c1?ABoolean
    equals true value

[<Fact>]
let ``Dynamic config operation must return TimeSpan`` () = 
    let c1 = config?outer 
    let (value: TimeSpan) = c1?ATime
    equals (TimeSpan.FromSeconds 30.) value

[<Fact>]
let ``Dynamic config operation must return float`` () = 
    let c1 = config?outer 
    let (value: float) = c1?SomeStrangeFloat
    equals 2.5 value

[<Fact>]
let ``Dynamic config operation must return double`` () = 
    let c1 = config?outer 
    let (value: double) = c1?SomeStrangeFloat
    equals 2.5 value

[<Fact>]
let ``Dynamic config operation must return decimal`` () = 
    let c1 = config?outer 
    let (value: decimal) = c1?SomeStrangeFloat
    equals 2.5M value

[<Fact>]
let ``Dynamic config operation must return string list`` () = 
    let c1 = config?outer 
    let (value: string seq) = c1?ListOfStrings
    equals ["hello";"hocon"] <| List.ofSeq value

[<Fact>]
let ``Dynamic config operation must return an enum`` () = 
    let c1 = config?outer 
    let (value: MyEnum) = c1?OptionType
    equals MyEnum.B value