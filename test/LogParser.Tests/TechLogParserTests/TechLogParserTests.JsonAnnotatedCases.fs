namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types
open LogParser.Dsl

type JsonAnnotatedCases() =

    /// 12
    static member JsonAnnotated : IEnumerable =
        seq {
            TestCaseData(
                """"foo": "Error: {
                        \"rabbitmq_node\":5672
                    }"
                """,
                TechJsonLogField.JsonAnnotated { Key = "foo"; Annotation = "Error:"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("12 - JsonAnnotated. simple int value")


            TestCaseData(
                "\\\"request\\\": DTO {rabbitmq_node:5672}",
                TechJsonLogField.JsonAnnotated { Key = "request"; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("12 - JsonAnnotated. simple quoteless int value")


            TestCaseData(
                "\\\"request\\\": DTO {rabbitmq_node: ABC}",
                TechJsonLogField.JsonAnnotated { Key = "request"; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" "ABC" }) }
            ).SetName("12 - JsonAnnotated. typeJson with string field without quotes test")
            
            
            TestCaseData(
                """Scoring: DeviceMetadataScoring { 
                       DeviceCountry: \"BR\"
                   }
                """,
                TechJsonLogField.JsonAnnotated
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