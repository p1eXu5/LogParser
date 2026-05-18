namespace LogParser.App.Tests

open System
open Microsoft.Extensions.Logging

open NUnit.Framework
open Faqt
open Faqt.Operators
open FSharp.Control.Reactive.Testing
open LogParser.Tests.Fakers
open p1eXu5.AspNetCore.Testing.Logging

open FSharp.Control.Reactive

open LogParser.App

module LogRepositoryTests =

    [<Test>]
    let ``ParseTextAck`` () =
        ()