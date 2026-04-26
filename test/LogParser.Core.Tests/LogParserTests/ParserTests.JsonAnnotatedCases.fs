namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type JsonAnnotatedCases() =

    /// 12
    static member JsonAnnotated : IEnumerable =
        seq {
            TestCaseData(
                """"foo": "Error: {
                        \"rabbitmq_node\":5672
                    }"
                """,
                TechField.JsonAnnotated { Key = "foo"; Annotation = "Error:"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("12 - JsonAnnotated. simple int value")


            TestCaseData(
                "\\\"request\\\": DTO {rabbitmq_node:5672}",
                TechField.JsonAnnotated { Key = "request"; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("12 - JsonAnnotated. simple quoteless int value")


            TestCaseData(
                "\\\"request\\\": DTO {rabbitmq_node: ABC}",
                TechField.JsonAnnotated { Key = "request"; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" "ABC" }) }
            ).SetName("12 - JsonAnnotated. typeJson with string field without quotes test")
            
            
            TestCaseData(
                """Scoring: DeviceMetadataScoring { 
                       DeviceCountry: \"BR\"
                   }
                """,
                TechField.JsonAnnotated
                    {
                        Key = "Scoring"
                        Annotation = "DeviceMetadataScoring"
                        Body =
                            (jsonLog {
                                Field "DeviceCountry" "BR"
                            })
                    }
            ).SetName("12 - JsonAnnotated. nested with no quotes")
        }