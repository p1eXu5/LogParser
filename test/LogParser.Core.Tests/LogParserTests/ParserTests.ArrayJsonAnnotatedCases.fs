namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type ArrayJsonAnnotatedCases() =

    /// 14
    static member ArrayJsonAnnotated : IEnumerable =
        seq {
            TestCaseData(
                "request: [DTO {rabbitmq_node:5672}]",
                TechField.ArrayJsonAnnotated ("request", [ { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) } ])
            ).SetName("14 - ArrayJsonAnnotated. simple int value")
        }