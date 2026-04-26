namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type JsonFieldCases() =

    /// 10
    static member JsonFields : IEnumerable =
        seq {
            TestCaseData(
                "\"foo\": \"{
                        \\\"rabbitmq_node\\\":5672
                    }\"",
                TechJsonField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 }))
            ).SetName("p_jsonField - Json. with simple int field")

            TestCaseData(
                "\"foo\": \"{
                        \\\"rabbitmq_node\\\":\\\"5672\\\"
                    }\"",
                TechJsonField.Json ("foo", (jsonLog { Field "rabbitmq_node" "5672" }))
            ).SetName("p_jsonField - Json. with simple string field")

            TestCaseData(
                "\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"",
                TechJsonField.Json ("requestBody", (jsonLog { Field "bankId" "1234" }))
            ).SetName("p_jsonField - Json. with escaped double quotes body")

            TestCaseData(
                "\"errorDetails\":\"{\\n  \\\"errorCode\\\": \\\"0005\\\",\\n  \\\"message\\\": \\\"Request rejected\\\"\\n}\"",
                TechJsonField.Json (
                        "errorDetails",
                        (jsonLog {
                            Field "errorCode" "0005"
                            message "Request rejected"
                        })
                )
            ).SetName("p_jsonField - Json. with escaped new line")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":5672
                    }
                """,
                TechJsonField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 }))
            ).SetName("p_jsonField - Json. with not escaped quotes")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":{
                            "bar": "5672"
                        }
                    }
                """,
                TechJsonField.Json (
                        "foo",
                        (jsonLog {
                            Field
                                "rabbitmq_node"
                                (jsonLog {
                                     Field "bar" "5672"
                                })
                        })
                )
            ).SetName("p_jsonField - Json. nested with not escaped quotes")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":{
                            "bar": {
                                "baz": "5672"
                            }
                        }
                    }
                """,
                TechJsonField.Json (
                        "foo",
                        (jsonLog {
                            Field "rabbitmq_node"
                                (jsonLog {
                                     Field "bar" 
                                        (jsonLog {
                                            Field "baz" "5672"
                                    })
                                })
                        })
                )
            ).SetName("p_jsonField - Json. nested^2 with not escaped quotes")

            TestCaseData(
                """"foo": "{
                        \"rabbitmq_node\":{
                            \"bar\": \"5672\"
                        }
                    }"
                """,
                TechJsonField.Json (
                        "foo",
                        (jsonLog {
                            Field
                                "rabbitmq_node"
                                (jsonLog {
                                     Field "bar" "5672"
                                })
                        })
                )
            ).SetName("p_jsonField - Json. nested")
        }

    /// 15
    static member FullMessage : IEnumerable =
        seq {
            TestCaseData(
                "\"fullMessage\":\"\"\"{\"traceId\":\"791b789add8cbd88c9bf1ea07d43d3f6\",\"0\":\"srv-mysqldb.metal.com\"}\"\"\"",
                TechJsonField.Json (
                        "fullMessage",
                        (jsonLog {
                            traceId "791b789add8cbd88c9bf1ea07d43d3f6"
                            Field "0" "srv-mysqldb.metal.com"
                        })
                )
            ).SetName("p_jsonField - Json. kibana fullMessage")

            TestCaseData(
                "\"fullMessage\": \"\"\"{
                    \"traceId\":\"791b789add8cbd88c9bf1ea07d43d3f6\",
                    \"0\":\"srv-mysqldb.metal.com\"
                }\"\"\"",
                TechJsonField.Json (
                        "fullMessage",
                        (jsonLog {
                            traceId "791b789add8cbd88c9bf1ea07d43d3f6"
                            Field "0" "srv-mysqldb.metal.com"
                        })
                )
            ).SetName("p_jsonField - Json. kibana fullMessage multi lined")

            TestCaseData(
                "\"fullMessage\": \"\"\"{\"timestamp\": \"2022-07-15T04:02:47.002Z\"}\"\"\"",
                TechJsonField.Json (
                        "fullMessage",
                        (jsonLog {
                            timestamp "2022-07-15T04:02:47.002Z"
                        })
                )
            ).SetName("p_jsonField - Json. kibana fullMessage with timestamp")
        }