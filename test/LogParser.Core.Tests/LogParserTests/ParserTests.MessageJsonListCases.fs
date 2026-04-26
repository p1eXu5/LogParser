namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type MessageJsonListCases() =

    /// 41
    static member MessageJsonList : IEnumerable =
        seq {
            TestCaseData(
                "[ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] ",
                TechJsonField.JsonAnnotated {
                    Key = "request"
                    Annotation = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        }
                }
                |> List.singleton
            ).SetName("MessageJsonAnnotatedList with escaped double quotes")

            TestCaseData(
                "[ ( \"request\": DTO { 
                    rabbitmq_node:5672
                } ) ] ",
                TechJsonField.JsonAnnotated {
                    Key = "request"
                    Annotation = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        }
                }
                |> List.singleton
            ).SetName("MessageJsonAnnotatedList with double quotes")

            TestCaseData(
                "[ ( \"request\": DTO { 
                    rabbitmq_node:5672,
                    DeviceMetadata: DeviceMetadata { 
                        DeviceCountry: \"BR\"
                    }
                } ) ] ",
                TechJsonField.JsonAnnotated {
                    Key = "request"
                    Annotation = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                            Field
                                "DeviceMetadata" "DeviceMetadata" (jsonLog { Field "DeviceCountry" "BR" })
                        }
                }
                |> List.singleton
            ).SetName("MessageJsonAnnotatedList with double quotes nested")
        }