namespace Akkling.Hocon

[<AutoOpen>]
module Role =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
        
    [<AbstractClass>]
    type RoleBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        /// Minimum required number of members of a certain role before the leader
        /// changes member status of 'Joining' members to 'Up'. Typically used together
        /// with 'Cluster.registerOnMemberUp' to defer some action, such as starting
        /// actors, until the cluster has reached a certain size.
        ///
        /// E.g. to require 2 nodes with role 'frontend' and 3 nodes with role 'backend':
        ///   frontend.min-nr-of-members = 2
        ///   backend.min-nr-of-members = 3
        ///
        /// You can apply multiples of thie field.
        [<CustomOperation("min_nr_of_members");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MinNrOfMembers (state: string list, role: string, number: int) =
            positiveField (sprintf "%s.min-nr-of-members" role) number::state
        /// Minimum required number of members of a certain role before the leader
        /// changes member status of 'Joining' members to 'Up'. Typically used together
        /// with 'Cluster.registerOnMemberUp' to defer some action, such as starting
        /// actors, until the cluster has reached a certain size.
        ///
        /// E.g. to require 2 nodes with role 'frontend' and 3 nodes with role 'backend':
        ///   frontend.min-nr-of-members = 2
        ///   backend.min-nr-of-members = 3
        ///
        /// You can apply multiples of thie field.
        member this.min_nr_of_members role number =
            this.MinNrOfMembers([], role, number)
            |> this.Run

    let role =
        { new RoleBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "role" 3 state
                |> Akka.Cluster.Field }
