﻿//-----------------------------------------------------------------------
// <copyright file="Consistency.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski <gttps://github.com/Horusiath>
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
