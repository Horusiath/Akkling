//-----------------------------------------------------------------------
// <copyright file="Configuration.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open System

[<AutoOpen>]
module HoconExtensions =
    open Akka.Configuration
    open System.Reflection

    let private toSnakeCase (s: string): string =
        if s = "" then s
        else 
            let src = s.ToCharArray()
            let dst = ResizeArray()
            for c in src do
                if (Char.IsUpper c) then 
                    dst.Add('-')
                    dst.Add(Char.ToLower c)
                else 
                    dst.Add(c)
            if dst.[0] = '-' then dst.RemoveAt(0)
            String(dst.ToArray())
            
    let (?) (conf: Config) (prop: string): 'a =
        let t = typeof<'a>
        let path = toSnakeCase prop
        if   t = typeof<Config> then box (conf.GetConfig(path)) :?> 'a
        elif t = typeof<string> then box (conf.GetString(path)) :?> 'a
        elif t = typeof<int> then box (conf.GetInt(path)) :?> 'a
        elif t = typeof<int64> then box (conf.GetLong(path)) :?> 'a
        elif t = typeof<bool> then box (conf.GetBoolean(path)) :?> 'a
        elif t = typeof<TimeSpan> then box (conf.GetTimeSpan(path)) :?> 'a
        elif t = typeof<float32> then box (conf.GetFloat(path)) :?> 'a
        elif t = typeof<float> then box (conf.GetDouble(path)) :?> 'a
        elif t = typeof<decimal> then box (conf.GetDecimal(path)) :?> 'a
        elif t = typeof<string seq> then box (conf.GetStringList(path)) :?> 'a
        elif t = typeof<int seq> then box (conf.GetIntList(path)) :?> 'a
        elif t = typeof<bool seq> then box (conf.GetBooleanList(path)) :?> 'a
        elif t = typeof<byte seq> then box (conf.GetByteList(path)) :?> 'a
        elif t = typeof<int64 seq> then box (conf.GetLongList(path)) :?> 'a
        elif t = typeof<float seq> then box (conf.GetFloatList(path)) :?> 'a
        elif t = typeof<double seq> then box (conf.GetDoubleList(path)) :?> 'a
        elif t = typeof<decimal seq> then box (conf.GetDecimalList(path)) :?> 'a
        elif typeof<Enum>.GetTypeInfo().IsAssignableFrom t then Enum.Parse(t, conf.GetString path, true) :?> 'a
        else failwithf "Result type of %O is not supported" t
        

[<RequireQualifiedAccess>]
module Configuration = 
    open Akka.Configuration

    let internal extendedConfig = (ConfigurationFactory.ParseString """
            akka.actor {
                serializers {
                    hyperion = "Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"
                }
                serialization-bindings {
                  "System.Object" = hyperion
                }
            }
        """)

    /// Parses provided HOCON string into a valid Akka configuration object.
    let parse = Akka.Configuration.ConfigurationFactory.ParseString
    
    /// Returns default Akka for F# configuration.
    let defaultConfig () = extendedConfig.WithFallback(Akka.Configuration.ConfigurationFactory.Default())
    
    /// Loads Akka configuration from the project's .config file.
    let load = Akka.Configuration.ConfigurationFactory.Load

    /// Sets a first argument as a fallback configuration of the second one.
    let inline fallback (other: Akka.Configuration.Config) (config: Akka.Configuration.Config) =
        config.WithFallback(other)

    //let readAs<'Record> (config: Config): 'Record = failwith "not implemented" //TODO: rewrite with typeshape