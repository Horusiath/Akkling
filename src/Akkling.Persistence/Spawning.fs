//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akkling
open Microsoft.FSharp.Quotations

module Linq = 
    open System.Linq.Expressions
    open Akkling.Linq
    
    type PersistentExpression = 
        
        static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunPersistentActor<'Command>>>) = 
            match f with
            | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) -> 
                Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<FunPersistentActor<'Command>>>
            | _ -> failwith "Doesn't match"
        
        static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunPersistentView<'Event>>>) = 
            match f with
            | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) -> 
                Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<FunPersistentView<'Event>>>
            | _ -> failwith "Doesn't match"

[<AutoOpen>]
module Props =

    /// <summary>
    /// Creates a props describing a way to incarnate persistent actor with behavior described by <paramref name="receive"/> function.
    /// </summary>
    let propsPersist (receive: Eventsourced<'Message> -> Effect<'Message>) : Props<'Message> =
        Props<'Message>.Create<FunPersistentActor<'Message>, Eventsourced<'Message>, 'Message>(receive)
        
    /// <summary>
    /// Creates a props describing a way to incarnate persistent actor with behavior described by <paramref name="expr"/> expression.
    /// </summary>
    let propsPersiste (expr: Expr<(Eventsourced<'Message> -> Effect<'Message>)>) : Props<'Message> =
        Props<'Message>.Create<FunPersistentActor<'Message>, Eventsourced<'Message>, 'Message>(expr)
        
    /// <summary>
    /// Creates a props describing a way to incarnate persistent view with behavior described by <paramref name="receive"/> function.
    /// </summary>
    let propsView (receive: View<'Message> -> Effect<'Message>) : Props<'Message> =
        Props<'Message>.Create<FunPersistentView<'Message>, View<'Message>, 'Message>(receive)
        
    /// <summary>
    /// Creates a props describing a way to incarnate persistent view with behavior described by <paramref name="expr"/> expression.
    /// </summary>
    let propsViewe (expr: Expr<(View<'Message> -> Effect<'Message>)>) : Props<'Message> =
        Props<'Message>.Create<FunPersistentView<'Message>, View<'Message>, 'Message>(expr)
