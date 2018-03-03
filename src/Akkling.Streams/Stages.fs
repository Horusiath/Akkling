//-----------------------------------------------------------------------
// <copyright file="Tcp.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Streams.Stages

open Akka.Streams
open Akka.Streams.Stage

[<Sealed>]
type StageLogic(shape: Shape, init) as this =
    inherit GraphStageLogic(shape)
    do 
        init(this)
    member this.Handler(inlet: Inlet<'a>, handler: #IInHandler) = this.SetHandler(inlet, handler)
    member this.Handler(inlet: Outlet<'a>, handler: #IOutHandler) = this.SetHandler(inlet, handler)
    member this.Pull(inlet) = base.Pull(inlet)
    member this.TryPull(inlet) = base.TryPull(inlet)
    member this.Cancel(inlet) = base.Cancel(inlet)
    member this.Grab(inlet) = base.Grab(inlet)
    member this.HasBeenPulled(inlet) = base.HasBeenPulled(inlet)
    member this.IsAvailable(inlet: Inlet<'a>) = base.IsAvailable(inlet)
    member this.IsAvailable(outlet: Outlet<'a>) = base.IsAvailable(outlet)
    member this.IsClosed(inlet: Inlet<'a>) = base.IsClosed(inlet)
    member this.IsClosed(outlet: Outlet<'a>) = base.IsClosed(outlet)
    member this.Push(outlet: Outlet<'a>, elem: 'a) = base.Push(outlet, elem)
    member this.SetKeepGoing(enabled) = base.SetKeepGoing(enabled)
    member this.Complete(outlet: Outlet<'a>) = base.Complete(outlet)
    member this.Fail(outlet: Outlet<'a>, error: #exn) = base.Push(outlet, error)
    member this.GetAsyncCallback(handler: 'a -> unit) = base.GetAsyncCallback(System.Action<'a>(handler))
    
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