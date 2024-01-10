## New in 0.14 (Released 2024/01/11)
* Upgraded Akka.NET dependencies to 1.5.15
* Revised internal TypedMessageExtractor type to support new version of IMessageExtractor
* Updated Xunit packages

## New in 0.13 (Released 2023/03/11)
* Upgraded Akka.NET dependencies to 1.5

## New in 0.10 (Released 2020/03/14)
* Upgraded Akka.NET dependencies to 1.4.2
* Consolidated supported .NET version to .NET Standard 2.0
* Removed PersistentView (as its no longer supported)
* Akkling.Streams: added support to stream refs, observables, cancellation tokens and withContext variants.
* Akkling.Streams: switched tuple args to value tuples to match Akka.Streams API.

## New in 0.9.3 (Released 2018/07/09)
* Rollback FSharp.Core to 4.3.4
* Include XML docs

## New in 0.9.2 (Released 2018/06/10)
* New computation expression for Graph DSL
* Renamed graph join operator from => to =>>
* ByteString module
* New streams stages: valve, pulse, managedDelay, partition, statefulPartition, mapMatValue

## New in 0.9.1 (Released 2018/03/17)
* Restored multi-framework target

## New in 0.9 (Released 2018/03/11)
* .NET Standard update
* Removed F# quotations from the API
* Removed FsPickler dependency
* Akka.Streams.TestKit package
* More akka streams API exposed (including Graph stages)

## New in 0.8 (Released 2017/11/27)
* Initial functioning GraphDSL API
* Akka.Streams TCP wrappers

## New in 0.7 (Released 2017/09/18)
* Updated to Akka.NET v1.3.1
* Added experimental EntityRef for cluster sharding
* Extended Akkling.DistributedData API
* Extended Akkling.Streams API
* Minor bug fixes

## New in 0.6.1 (Released 2017/04/17)
* Fixed external package dependencies.

## New in 0.6 (Released 2017/04/17)
* Updated dependencies to match Akka.NET v1.2
* Updated features for Akka.Streams

## New in 0.5 (Released 2017/01/25)
* Switched to Hyperion as default serializer
* Adapter library for Akka.DistributedData

## New in 0.4.2 (Released 2017/01/03)
* Fixed Akka.NET dependency

## New in 0.4.1 (Released 2017/01/03)
* Relaxed dependency on System.Collections.Immutable
* Minor fix for persistent actors for unhandled messages

### New in 0.4.0 (Released 2016/09/11)
* Non-blocking interop with async computation expression
* Initial Akkling.Streams plugin
* Support for at-least-once-delivery semantic
* More examples

### New in 0.3.0 (Released 2016/02/25)
* Akkling.Cluster.Sharding and Akkling.TestKit packages
* new typed Props, all `spawn` functions now operates on them
* `actorOf` functions now returns effects
* new `become` effect
* new effect combinators `<|>` and `<&>`
* Ask operator `<?` now returns async of `AskResult<'msg>`

### New in 0.2.1 (Released 2015/12/17)
* Forward operator `<<!`
* Parent property in actor contexts
* Split Persist/PersistAsyn effect into single- and multi-event versions
* Initialized native F# support for Akka.IO (Akkling.IO namespace)
* F# support for some of the Akka system messages in form of active patterns.
* Akkling.Behaviors module with set of common behaviors.

### New in 0.2.0 (Released 2015/11/20)
* New effects-based actor expression API
* New Persistence API based on effects
* Actor/persistent actor lifecycle events handled as messages
* using Akka nightly builds
* switched to Wire as default serializer
* minor function arguments precedence redesign

### New in 0.1.1 (Released 2015/08/08)
* Fixed problem with Discriminated Unions serialization
* Upgraded to Akka 1.0.4 and FsPickler 1.2

### New in 0.1.0 (Released 2015/06/25)
* Upgraded Newtonsoft.Json dependency to 7.0.1
* Akkling.Persistence package released

#### New in 0.1.0-beta (Released 2015/06/13)
* Initial release
* Typed actor refs
* Simplified API
