namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type JsonAnnotatedValueCases() =

    /// 13
    static member JsonAnnotatedValue : IEnumerable =
        seq {
            TestCaseData(
                "DTO {rabbitmq_node:5672}",
                { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("13 - JsonAnnotatedValue. simple int value")
        }