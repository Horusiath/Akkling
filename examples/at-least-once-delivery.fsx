#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling/bin/Debug/System.Collections.Immutable.dll"
#r "../src/Akkling.Persistence/bin/Debug/Google.ProtocolBuffers.dll"
#r "../src/Akkling.Persistence/bin/Debug/Akka.Persistence.dll"
#r "../src/Akkling.Persistence/bin/Debug/Akkling.Persistence.dll"

open System
open Akka.Persistence
open Akkling
open Akkling.Persistence

(* Delivery mechanism looks like this - if sender wants to reliably deliver payload to recipient 
   using at-least-once delivery semantics, it sends that payload wrapped to Messenger actor, which 
   is responsible for the persistence and redelivery:
   
    +--------+                      +-----------+                    +-----------+
    |        |--(DeliverOrder<T>)-->|           |--(Delivery<T>:1)-->|           |
    |        |                      |           |  /* 2nd attempt */ |           |
    | Sender |                      | Messenger |--(Delivery<T>:2)-->| Recipient |
    |        |                      |           |                    |           |
    |        |                      |           |<----(Confirm:2)----|           |
    +--------+                      +-----------+                    +-----------+
*)

type Delivery = { Payload: string; DeliveryId: int64 }

type Cmd =
    | DeliverOrder of string * IActorRef<Delivery>
    | Confirm of int64

type Evt =
    | MessageSent of string * IActorRef<Delivery> 
    | Confirmed of int64

let system = System.create "system" <| Configuration.defaultConfig()

let messenger = 
    spawn system "counter-1" (propsPersist(fun mailbox ->
        let deliverer = AtLeastOnceDelivery.createDefault mailbox
        let rec loop () = 
            actor {
                let! msg = mailbox.Receive()
                let effect = deliverer.Receive (upcast mailbox) msg
                if effect.WasHandled()
                then return effect
                else 
                    match msg with
                    | SnapshotOffer snap ->
                        deliverer.DeliverySnapshot <- snap
                    | :? Cmd as cmd ->
                        match cmd with
                        | DeliverOrder(payload, recipient) ->
                            return Persist(upcast MessageSent(payload, recipient))
                        | Confirm(deliveryId) ->
                            return Persist(upcast Confirmed(deliveryId))
                    | :? Evt as evt ->
                        match evt with
                        | MessageSent(payload, recipient) ->
                            let fac d = { Payload = payload; DeliveryId = d }
                            return deliverer.Deliver(recipient.Path, fac, mailbox.IsRecovering()) |> ignored
                        | Confirmed(deliveryId) ->
                            printfn "Message %d confirmed" deliveryId
                            return deliverer.Confirm deliveryId |> ignored
                    | _ -> return Unhandled
            }
        loop ())) 
    |> retype

let echo = spawn system "echo" <| props(actorOf2(fun mailbox msg ->
    printfn "Received: %s. Confirming" msg.Payload
    mailbox.Sender() <! Confirm msg.DeliveryId
    Ignore))

messenger <! DeliverOrder("hello world", echo)