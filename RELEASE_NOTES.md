## New in 0.4.1 (Released 2016/12/22)
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
