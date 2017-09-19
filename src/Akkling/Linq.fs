//-----------------------------------------------------------------------
// <copyright file="Linq.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Linq  
    
open System.Linq.Expressions
open Microsoft.FSharp.Linq

let (|Lambda|_|) (e : Expression) = 
    match e with
    | :? LambdaExpression as l -> Some(l.Parameters, l.Body)
    | _ -> None

let (|Call|_|) (e : Expression) = 
    match e with
    | :? MethodCallExpression as c -> Some(c.Object, c.Method, c.Arguments)
    | _ -> None

let (|Method|) (e : System.Reflection.MethodInfo) = e.Name

let (|Invoke|_|) = 
    function 
    | Call(o, Method("Invoke"), _) -> Some o
    | _ -> None

let (|Ar|) (p : System.Collections.ObjectModel.ReadOnlyCollection<Expression>) = Array.ofSeq p

let toExpression<'Actor> (f : System.Linq.Expressions.Expression) = 
    match f with
    | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) | Call(null, Method "ToFSharpFunc", 
                                                                                         Ar [| Lambda(_, p) |]) -> 
        Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<'Actor>>
    | _ -> failwith "Doesn't match"

type Expression = 
    static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunActor<'Message>>>) = 
        toExpression<FunActor<'Message>> f
    // static member ToExpression<'Actor>(f : Quotations.Expr<unit -> 'Actor>) = 
    //     toExpression<'Actor> (QuotationEvaluator.ToLinqExpression f)