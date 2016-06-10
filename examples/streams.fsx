#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling.Streams/bin/Debug/Reactive.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/Akka.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/Akkling.Streams.dll"

open System
open Akka.Streams
open Akkling
open Akkling.Streams

let text = """
       Lorem Ipsum is simply dummy text of the printing and typesetting industry.
       Lorem Ipsum has been the industry's standard dummy text ever since the 1500s,
       when an unknown printer took a galley of type and scrambled it to make a type
       specimen book."""

let system = System.create "streams-sys" <| Configuration.defaultConfig()
let mat = system.Materializer()

Source.ofArray (text.Split())
|> Source.map (fun x -> x.ToUpper())
|> Source.runForEach mat (printfn "%s")
|> Async.RunSynchronously