namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types
open LogParser.Dsl

type JsonAnnotatedValueCases() =

    /// 13
    static member JsonAnnotatedValue : IEnumerable =
        seq {
            TestCaseData(
                "DTO {rabbitmq_node:5672}",
                { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("13 - JsonAnnotatedValue. simple int value")
        }