#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FsPickler.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"

open System
open Akkling

let system = System.create "basic-sys" <| Configuration.defaultConfig()

let helloRef = spawn system "hello-actor" <| fun m ->
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
    
helloRef <! "ok"
helloRef <! "unhandle"
helloRef <! "ok"
helloRef <! "stop"
helloRef <! "ok"