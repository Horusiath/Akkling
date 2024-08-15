//-----------------------------------------------------------------------
// <copyright file="Consistency.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.DistributedData.Consistency

open Akka.DistributedData

let readLocal = ReadLocal.Instance
let readFrom n timeout = ReadFrom(n, timeout)
let readMajority timeout = ReadMajority(timeout)
let readAll timeout = ReadAll(timeout)

let writeLocal = WriteLocal.Instance
let writeTo n timeout = WriteTo(n, timeout)
let writeMajority timeout = WriteMajority(timeout)
let writeAll timeout = WriteAll(timeout)
