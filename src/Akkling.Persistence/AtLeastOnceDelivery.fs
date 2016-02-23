﻿//-----------------------------------------------------------------------
// <copyright file="PersistentView.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Persistence.AtLeastOnceDelivery

open System
open Akka.Actor
open Akka.Persistence
open Akkling
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation
 