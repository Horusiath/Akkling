open Akkling

let system = System.create "system" <| Configuration.parse """
    akka {
        loglevel = DEBUG
        actor {
            serialization-bidnings {
                "System.Object" = wire
            }
        }
        debug {
          receive = on
          autoreceive = on
          lifecycle = on
          fsm = on
          event-stream = on
          unhandled = on
          router-misconfiguration = on
        }
    }
"""

let aref = 
    spawn system "actor" 
    <| fun ctx ->
        let rec loop () =
            actor {
                let! m = ctx.Receive()
                match m with
                | "stop" -> return Stop
                | "unhandled" -> return Unhandled
                | x -> 
                    printfn "Received: %s" x
                    return! loop ()
            }
        loop ()

aref <! "ok"
aref <! "unhandled"
aref <! "stop"
aref <! "ok"


System.Console.ReadLine ()