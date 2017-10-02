namespace FSharp.Quotations.Evaluator

open Microsoft.FSharp.Quotations

module QuotationEvaluator =
    let inline Evaluate (expr: Expr<'T>) :'T =
        raise <| System.NotImplementedException "Waiting for netstandard build of FSharp.Quotations.Evaluator"
  
