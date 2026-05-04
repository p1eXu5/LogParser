namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types
open LogParser.Dsl

type ArrayJsonAnnotatedCases() =

    /// 14
    static member ArrayJsonAnnotated : IEnumerable =
        seq {
            TestCaseData(
                "request: [DTO {rabbitmq_node:5672}]",
                TechJsonLogField.ArrayJsonAnnotated ("request", [ { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) } ])
            ).SetName("14 - ArrayJsonAnnotated. simple int value")
        }