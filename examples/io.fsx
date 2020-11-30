#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akkling"

open System
open Akkling
open Akkling.IO
open Akkling.IO.Tcp
open System.Net
let system = System.create "telnet-sys" <| Configuration.defaultConfig()
let handler connection = fun (ctx: Actor<obj>) ->
    monitor ctx connection |> ignore
    let rec loop () = actor {
        let! msg = ctx.Receive ()
        match msg with
        | Received(data) ->
            printfn "%s" (string data)
            return! loop ()
        | Terminated(_, _,_) | ConnectionClosed(_) -> return Stop
        | _ -> return Unhandled
    }
    loop ()

let endpoint = IPEndPoint(IPAddress.Loopback, 5000)
let listener = spawn system "listener" <| props(fun m ->
    IO.Tcp(m) <! TcpMessage.Bind(untyped m.Self, endpoint, 100)
    let rec loop () = actor {
        let! (msg: obj) = m.Receive ()
        match msg with
        | Connected(remote, local) ->
            let conn = m.Sender ()
            conn <! TcpMessage.Register(untyped (spawn m null (props(handler conn))))
            return! loop ()
        | _ -> return Unhandled
    }
    loop ())
