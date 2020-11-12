namespace Akkling.Hocon

open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module SharedInternal =
    type State<'T> =
        { Target: 'T
          Fields: string list }

    [<RequireQualifiedAccess>]
    module State =
        let create (defaultTarget: 'T) (fields: string list) =
            { Target = defaultTarget; Fields = fields }

        let add (state: State<'T>) (field: string) =
            { state with Fields = field::state.Fields }

        let addWith (state: State<'T>) (target: 'T) (field: string) =
            { Target = target; Fields = field::state.Fields }
