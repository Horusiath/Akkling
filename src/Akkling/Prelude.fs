[<RequireQualifiedAccess>]
module internal Akkling.Option

open System

let toNullable = 
    function 
    | Some x -> Nullable x
    | None -> Nullable()
