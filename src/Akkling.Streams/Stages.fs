//-----------------------------------------------------------------------
// <copyright file="Tcp.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Streams.Stages

open Akka.Actor
open Akka.Streams
open Akka.Streams.Stage
open Akkling

[<Sealed>]
type StageLogic(shape: Shape, init) as this =
    inherit GraphStageLogic(shape)
    do 
        init(this)
    member this.Handler(inlet: Inlet<'a>, handler: #IInHandler): unit = this.SetHandler(inlet, handler)
    member this.Handler(inlet: Outlet<'a>, handler: #IOutHandler): unit = this.SetHandler(inlet, handler)
    member this.Pull(inlet: Inlet<'a>): unit = base.Pull(inlet)
    member this.TryPull(inlet: Inlet<'a>): unit = base.TryPull(inlet)
    member this.Cancel(inlet: Inlet<'a>): unit = base.Cancel(inlet)
    member this.Grab(inlet: Inlet<'a>): 'a = base.Grab(inlet)
    member this.HasBeenPulled(inlet: Inlet<'a>): bool = base.HasBeenPulled(inlet)
    member this.IsAvailable(inlet: Inlet<'a>): bool  = base.IsAvailable(inlet)
    member this.IsAvailable(outlet: Outlet<'a>): bool  = base.IsAvailable(outlet)
    member this.IsClosed(inlet: Inlet<'a>): bool  = base.IsClosed(inlet)
    member this.IsClosed(outlet: Outlet<'a>): bool  = base.IsClosed(outlet)
    member this.Push(outlet: Outlet<'a>, elem: 'a): unit = base.Push(outlet, elem)
    member this.SetKeepGoing(enabled: bool): unit = base.SetKeepGoing(enabled)
    member this.Complete(outlet: Outlet<'a>): unit = base.Complete(outlet)
    member this.Fail(outlet: Outlet<'a>, error: #exn): unit = base.Fail(outlet, error)
    member this.FailStage(error: #exn): unit = base.FailStage(error)
    member this.GetAsyncCallback(handler: 'a -> unit): ('a -> unit) = base.GetAsyncCallback(System.Action<'a>(handler)).Invoke
    member this.StageActorRef<'b>(receive: IActorRef<'b> -> obj -> unit) = 
        base.GetStageActor(StageActorRef.Receive(fun (struct(aref, msg)) -> receive (typed aref) msg))
    member this.Emit(outlet: Outlet<'a>, elem: 'a): unit = base.Emit(outlet, elem)
    
let inline graphStagelogic (shape: #Shape) (init: StageLogic -> unit): GraphStageLogic = upcast StageLogic(shape, init)

[<Sealed>]
type Deduplicate<'a>(eq: 'a -> 'a -> bool) =
    inherit GraphStage<FlowShape<'a, 'a>>()
    
    let mutable last: Option<'a> = None
    let inlet = Inlet<'a>("deduplicate.in")
    let outlet = Outlet<'a>("deduplicate.out")
    
    override this.Shape = FlowShape(inlet, outlet)
    override this.CreateLogic attrs = 
        graphStagelogic this.Shape (fun logic -> 
            logic.Handler(inlet, { new InHandler() with
                override x.OnPush() = 
                    let next = logic.Grab(inlet)
                    match last with
                    | Some (l) when eq l next ->
                        logic.Pull(inlet)
                    | _ -> 
                        logic.Push(outlet, next)
                        last <- Some next
            })
            
            logic.Handler(outlet, { new OutHandler() with
                override x.OnPull() = logic.Pull(inlet)
            })
        )