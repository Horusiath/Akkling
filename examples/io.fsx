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
            ctx.Sender() <! TcpMessage.Write(ByteString.ofUtf8String("ACK: " + (string data)))
            return! loop ()
        | Terminated(_, _, _) | ConnectionClosed(_) -> return Stop
        | _ -> return Ignore
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
        | _ -> return Ignore
    }
    loop ())


// Ex: Initiate connection & communication via a client
//     existing outside the actor system.

open System.Text
open System.Threading.Tasks
open System.Net.Sockets

let echo (msg: string) = task {
    use client = new TcpClient()
    do! client.ConnectAsync(endpoint)
    use stream = client.GetStream()

    do! stream.WriteAsync(Encoding.UTF8.GetBytes msg)

    let buffer = Array.create 1024 (byte 1)
    let! received = stream.ReadAsync buffer
    let result = Encoding.UTF8.GetString(buffer, 0, received)

    printfn "Client request result: %A" result
}

Task.Delay(1500).Wait()
(echo "big fish").Wait()
(echo "small fish").Wait()

// Alternatively, use telnet from the console:
// > telnet 127.0.0.1 5000
// > starfish
// > ACK: starfish
