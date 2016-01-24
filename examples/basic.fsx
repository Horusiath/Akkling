#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig()

let behavior (m:Actor<_>) = 
    let rec loop () = actor {
        let! msg = m.Receive ()
        match msg with
        | "stop" -> return Stop
        | "unhandle" -> return Unhandled
        | x -> 
            printfn "%s" x
            return! loop ()
    }
    loop () 

// First approach - using explicit behavior loop
let helloRef = spawnAnonymous system (props behavior)

// Second approach - using implicits
let helloBehavior = function
    | "stop" -> stop ()
    | "unhandle" -> unhandled ()
    | x -> printfn "%s" x |> ignored 

let helloRef2 = spawn system "hello-actor2" <| props (actorOf helloBehavior)
    
helloRef <! "ok"
helloRef <! "unhandle"
helloRef <! "ok"