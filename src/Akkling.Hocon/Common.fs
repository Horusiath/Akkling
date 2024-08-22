namespace Akkling.Hocon

open System
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module InternalHocon =
    let inline notNegative name (x: 'T) =
        if x < LanguagePrimitives.GenericZero<'T> then
            failwithf "Cannot input negative value for field %s: %s" name (string x)

    [<RequireQualifiedAccess>]
    module private List =
        let distinctSequential (xs: 'T list) =
            []
            |> List.foldBack
                (fun x xs ->
                    match xs with
                    | [] -> [ x ]
                    | [ h ] when h = x -> xs
                    | h :: _ when h = x -> xs
                    | _ -> x :: xs)
                xs

    type D1 = D1
    type D2 = D2
    type D3 = D3
    type D4 = D4
    type D5 = D5
    type D6 = D6
    type D7 = D7
    type D8 = D8
    type D9 = D9
    type D10 = D10
    type D11 = D11
    type D12 = D12
    type D13 = D13
    type D14 = D14
    type D15 = D15
    type D16 = D16
    type D17 = D17
    type D18 = D18

    type DurationToText = DurationToText
        with



            static member (&.)(DurationToText, x: float<ns>) =
                fun D1 _ _ _ _ _ _ _ -> string x + " nanoseconds"

            static member (&.)(DurationToText, x: int<ns>) =
                fun D1 _ _ _ _ _ _ _ -> string x + " nanoseconds"

            static member (&.)(DurationToText, x: float<us>) =
                fun D1 D2 _ _ _ _ _ _ -> string x + " microseconds"

            static member (&.)(DurationToText, x: int<us>) =
                fun D1 D2 _ _ _ _ _ _ -> string x + " microseconds"

            static member (&.)(DurationToText, x: float<ms>) =
                fun D1 D2 D3 _ _ _ _ _ -> string x + " milliseconds"

            static member (&.)(DurationToText, x: int<ms>) =
                fun D1 D2 D3 _ _ _ _ _ -> string x + " milliseconds"

            static member (&.)(DurationToText, x: float<s>) =
                fun D1 D2 D3 D4 _ _ _ _ -> string x + " seconds"

            static member (&.)(DurationToText, x: int<s>) =
                fun D1 D2 D3 D4 _ _ _ _ -> string x + " seconds"

            static member (&.)(DurationToText, x: float<m>) =
                fun D1 D2 D3 D4 D5 _ _ _ -> string x + " minutes"

            static member (&.)(DurationToText, x: int<m>) =
                fun D1 D2 D3 D4 D5 _ _ _ -> string x + " minutes"

            static member (&.)(DurationToText, x: float<h>) =
                fun D1 D2 D3 D4 D5 D6 _ _ -> string x + " hours"

            static member (&.)(DurationToText, x: int<h>) =
                fun D1 D2 D3 D4 D5 D6 _ _ -> string x + " hours"

            static member (&.)(DurationToText, x: float<d>) =
                fun D1 D2 D3 D4 D5 D6 D7 _ -> string x + " days"

            static member (&.)(DurationToText, x: int<d>) =
                fun D1 D2 D3 D4 D5 D6 D7 _ -> string x + " days"

    let inline durationToText x : string =
        (Unchecked.defaultof<DurationToText> &. x) D1 D2 D3 D4 D5 D6 D7 D8

    type BytesToText = BytesToText
        with



            static member (&.)(BytesToText, x: int<B>) =
                fun D1 _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " bytes"

            static member (&.)(BytesToText, x: int<kB>) =
                fun D1 D2 _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " kilobytes"

            static member (&.)(BytesToText, x: int<KiB>) =
                fun D1 D2 D3 _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " kibibytes"

            static member (&.)(BytesToText, x: int<MB>) =
                fun D1 D2 D3 D4 _ _ _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " megabytes"

            static member (&.)(BytesToText, x: int<MiB>) =
                fun D1 D2 D3 D4 D5 _ _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " mebibytes"

            static member (&.)(BytesToText, x: int<GB>) =
                fun D1 D2 D3 D4 D5 D6 _ _ _ _ _ _ _ _ _ _ _ _ -> string x + " gigabytes"

            static member (&.)(BytesToText, x: int<GiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 _ _ _ _ _ _ _ _ _ _ _ -> string x + " gibibytes"

            static member (&.)(BytesToText, x: int<TB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 _ _ _ _ _ _ _ _ _ _ -> string x + " terabytes"

            static member (&.)(BytesToText, x: int<TiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 _ _ _ _ _ _ _ _ _ -> string x + " tebibytes"

            static member (&.)(BytesToText, x: int<PB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 _ _ _ _ _ _ _ _ -> string x + " petabytes"

            static member (&.)(BytesToText, x: int<PiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 _ _ _ _ _ _ _ -> string x + " pebibytes"

            static member (&.)(BytesToText, x: int<EB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 _ _ _ _ _ _ -> string x + " exabytes"

            static member (&.)(BytesToText, x: int<EiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 _ _ _ _ _ -> string x + " exbibytes"

            static member (&.)(BytesToText, x: int<ZB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 D14 _ _ _ _ -> string x + " zettabytes"

            static member (&.)(BytesToText, x: int<ZiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 D14 D15 _ _ _ -> string x + " zebibytes"

            static member (&.)(BytesToText, x: int<YB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 D14 D15 D16 _ _ -> string x + " yottabytes"

            static member (&.)(BytesToText, x: int<YiB>) =
                fun D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 D14 D15 D16 D17 _ -> string x + " yobibytes"

    let inline bytesToText x : string =
        (Unchecked.defaultof<BytesToText> &. x) D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13 D14 D15 D16 D17 D18

    [<Literal>]
    let Off = "off"

    [<Literal>]
    let On = "on"

    let nl = Environment.NewLine
    let newLine x = x + nl
    let wrapQuotes x = "\"" + x + "\""
    let boolSwitch b = if b then On else Off

    let indent i s =
        if s <> nl then String.replicate i "    " + s else s

    let field name x =
        if String.IsNullOrEmpty x then
            sprintf "%s = \"\"" name
        else
            sprintf "%s = %s" name x

    let quotedField name x = wrapQuotes x |> field name
    let quotedNameField name x = field (wrapQuotes name) x
    let switchField name b = boolSwitch b |> field name
    let boolField name b = sprintf "%s = %b" name b

    let inline durationField name i =
        notNegative name i
        field name (durationToText i)

    let inline byteField name i =
        notNegative name i
        field name (bytesToText i)

    let inline numField name i = sprintf "%s = %i" name i

    let inline numFieldf name i = sprintf "%s = %g" name i

    let inline positiveField name i =
        notNegative name i
        numField name i

    let inline positiveFieldf name i =
        notNegative name i
        numFieldf name i

    let inline portField name i =
        if i > int UInt16.MaxValue then
            failwithf "Port invalid: %i" i

        positiveField name i

    let listField name (x: string list) =
        match x with
        | []
        | [ "" ] -> sprintf "%s = []" name
        | _ -> String.concat "," x |> sprintf "%s = [ %s ]" name

    let quotedListField name (x: string list) =
        List.map (wrapQuotes) x |> listField name

    let objExpr name indentLevel (body: string list) =
        let body =
            match body with
            | [] -> body
            | [ h ] when h.Trim() = nl -> []
            | h :: t when h.Trim() = nl ->
                match List.rev t with
                | [] -> t
                | [ h ] when h.Trim() = nl -> []
                | h :: t when h.Trim() = nl -> t
                | _ -> t
            | _ -> body
            |> List.distinctSequential
            |> List.map (indent indentLevel)
            |> List.distinct
            |> String.concat nl

        sprintf "%s {%s%s%s%s}" name nl body nl (indent (indentLevel - 1) "")

    let pathObjExpr (mkField: string -> 'Field) path indent name (body: string list) =
        objExpr (sprintf "%s.%s" path name) indent body |> mkField

    let stringyObjExpr name indentLevel (body: (string * string) list) =
        List.distinct body
        |> List.map (fun (n, v) -> quotedNameField n v)
        |> objExpr name indentLevel

    /// Fully qualified type name string.
    let fqcn (t: Type) =
        String.Format("{0}, {1}", t.FullName, t.Assembly.GetName().Name)

    [<AbstractClass; NoEquality; NoComparison>]
    type BaseBuilder<'T when 'T :> MarkerClasses.IField>() =
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: string) = [ x ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(a: unit) = []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine(state: string list, x: string list) = x @ state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero() = []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f ()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.For(xs: string list, f: unit -> string list) = f () @ xs

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run: string list -> 'T
