namespace LogParser.Core.Tests.Factories

open NUnit.Framework
open LogParser.Core.Types
open LogParser.Core.Dsl
open System.Collections


type JsonAndBodyFieldTestCases() =

    /// 10
    static member JsonFields : IEnumerable =
        seq {
            TestCaseData(
                "\"foo\": \"{
                        \\\"rabbitmq_node\\\":5672
                    }\"",
                TechField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 }))
            ).SetName("10 - Json. with simple int field")

            TestCaseData(
                "\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"",
                TechField.Json ("requestBody", (jsonLog { Field "bankId" "1234" }))
            ).SetName("10 - Json. with escaped double quotes body")

            TestCaseData(
                "\"errorDetails\":\"{\\n  \\\"errorCode\\\": \\\"0005\\\",\\n  \\\"message\\\": \\\"Request rejected\\\"\\n}\"",
                TechField.Json (
                        "errorDetails",
                        (jsonLog {
                            Field "errorCode" "0005"
                            message "Request rejected"
                        })
                )
            ).SetName("10 - Json. with escaped new line")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":5672
                    }
                """,
                TechField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 }))
            ).SetName("10 - Json. with not escaped quotes")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":{
                            "bar": "5672"
                        }
                    }
                """,
                TechField.Json (
                        "foo",
                        (jsonLog {
                            Field
                                "rabbitmq_node"
                                (jsonLog {
                                     Field "bar" "5672"
                                })
                        })
                )
            ).SetName("10 - Json. nested with not escaped quotes")

            TestCaseData(
                """"foo": {
                        "rabbitmq_node":{
                            "bar": {
                                "baz": "5672"
                            }
                        }
                    }
                """,
                TechField.Json (
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
            ).SetName("10 - Json. nested^2 with not escaped quotes")

            TestCaseData(
                """"foo": "{
                        \"rabbitmq_node\":{
                            \"bar\": \"5672\"
                        }
                    }"
                """,
                TechField.Json (
                        "foo",
                        (jsonLog {
                            Field
                                "rabbitmq_node"
                                (jsonLog {
                                     Field "bar" "5672"
                                })
                        })
                )
            ).SetName("10 - Json. nested")


        }

    static member ArrayJsonAnnonimous : IEnumerable =
        seq {
            TestCaseData(
                """[
                        {
                            "rabbitmq_node":{
                                "bar": "5672"
                            }
                        }
                    ]
                """,
                TechField.ArrayJsonAnnonimous [
                    jsonLog {
                        Field "rabbitmq_node" 
                            (jsonLog { Field "bar" "5672" })
                    }
                ]
            ).SetName("21 - ArrayJsonAnnonimous. single json")
        }

    /// 11
    static member ArrayJson : IEnumerable =
        seq {
            TestCaseData(
                """"foo": [
                        {
                            "rabbitmq_node":{
                                "bar": "5672"
                            }
                        }
                    ]
                """,
                TechField.ArrayJson (
                    "foo",
                    [
                        jsonLog {
                            Field "rabbitmq_node" 
                                (jsonLog { Field "bar" "5672" })
                        }
                    ]
                )
            ).SetName("11 - ArrayJson. with not escaped quotes")


            TestCaseData(
                """\"certificates\":[ { \"usage\":\"CA\" } ]
                """,
                TechField.ArrayJson (
                    "certificates",
                    [
                        jsonLog { Field "usage" "CA" }
                    ]
                )
            ).SetName("11 - ArrayJson. with escaped quotes single line")


            TestCaseData(
                """\"certificates\":[ 
                        { \"usage\":\"CA\" } 
                    ]
                """,
                TechField.ArrayJson (
                    "certificates",
                    [
                        jsonLog { Field "usage" "CA" }
                    ]
                )
            ).SetName("11 - ArrayJson. with escaped quotes multi lines")


            TestCaseData(
                """"foo": [[
                        {
                            "rabbitmq_node":{
                                "bar": "5672"
                            }
                        }
                    ]]
                """,
                TechField.ArrayJson (
                    "foo",
                    [
                        [TechField.ArrayJsonAnnonimous [
                            jsonLog {
                                Field "rabbitmq_node" 
                                    (jsonLog { Field "bar" "5672" })
                            }
                        ]]
                    ]
                )
            ).SetName("11 - ArrayJson. nested json array")

            TestCaseData(
                """"vals": [
                        "some message",
                        0,
                        "0ca140ed-2608-42a5-a0da-6c2665f88a65",
                        {
                            "HTTPHeader_content-type":"application/json",
                            "HTTPHeader_x-request-id":"0ca140ed-2608-42a5-a0da-6c2665f88a65",
                            "HTTPPath":"/debitint"
                        },
                        null
                    ]
                """,
                TechField.ArrayJson (
                    "vals",
                    [
                        [TechField.StringAnnonimous "some message";]
                        [TechField.IntAnnonimous 0;]
                        [TechField.StringAnnonimous "0ca140ed-2608-42a5-a0da-6c2665f88a65";]
                        
                        jsonLog {
                            Field "HTTPHeader_content-type" "application/json"
                            Field "HTTPHeader_x-request-id" "0ca140ed-2608-42a5-a0da-6c2665f88a65"
                            Field "HTTPPath" "/debitint"
                        }
                        
                        [TechField.NullAnnonimous]
                    ]
                )
            ).SetName("11 - ArrayJson. mix of nested json, string, int and null")
        }

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

    /// 13
    static member JsonAnnotatedValue : IEnumerable =
        seq {
            TestCaseData(
                "DTO {rabbitmq_node:5672}",
                { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) }
            ).SetName("13 - JsonAnnotatedValue. simple int value")
        }

    /// 14
    static member ArrayJsonAnnotated : IEnumerable =
        seq {
            TestCaseData(
                "request: [DTO {rabbitmq_node:5672}]",
                TechField.ArrayJsonAnnotated ("request", [ { Key = ""; Annotation = "DTO"; Body = (jsonLog { Field "rabbitmq_node" 5672 }) } ])
            ).SetName("14 - ArrayJsonAnnotated. simple int value")
        }

    /// 15
    static member FullMessage : IEnumerable =
        seq {
            TestCaseData(
                "\"fullMessage\":\"\"\"{\"traceId\":\"791b789add8cbd88c9bf1ea07d43d3f6\",\"0\":\"srv-mysqldb.metal.com\"}\"\"\"",
                TechField.Json (
                        "fullMessage",
                        (jsonLog {
                            traceId "791b789add8cbd88c9bf1ea07d43d3f6"
                            Field "0" "srv-mysqldb.metal.com"
                        })
                )
            ).SetName("15 - Json. kibana fullMessage")

            TestCaseData(
                "\"fullMessage\": \"\"\"{
                    \"traceId\":\"791b789add8cbd88c9bf1ea07d43d3f6\",
                    \"0\":\"srv-mysqldb.metal.com\"
                }\"\"\"",
                TechField.Json (
                        "fullMessage",
                        (jsonLog {
                            traceId "791b789add8cbd88c9bf1ea07d43d3f6"
                            Field "0" "srv-mysqldb.metal.com"
                        })
                )
            ).SetName("15 - Json. kibana fullMessage multi lined")

            TestCaseData(
                "\"fullMessage\": \"\"\"{\"timestamp\": \"2022-07-15T04:02:47.002Z\"}\"\"\"",
                TechField.Json (
                        "fullMessage",
                        (jsonLog {
                            timestamp "2022-07-15T04:02:47.002Z"
                        })
                )
            ).SetName("15 - Json. kibana fullMessage with timestamp")
        }


    /// 20
    static member BodyFields =
        seq {
            TestCaseData(
                "\"body\": \"{ \\\"TrId\\\": \\\"16700010001\\\" } \"",
                TechField.Body (jsonLog { Field "TrId" "16700010001" })
            ).SetName("20 - Body. simple")

            TestCaseData(
                """"body": "
                        {
                            \"rabbitmq_node\": \"5672\"
                        }"
                """,
                TechField.Body (jsonLog { Field "rabbitmq_node" "5672" })
            ).SetName("20 - Body. simple multi lined")

            TestCaseData(
                """"body":"
                        {
                            \"rabbitmq_node\": \"5672\"
                        }"
                """,
                TechField.Body (jsonLog { Field "rabbitmq_node" "5672" })
            ).SetName("20 - Body. simple multi lined 2")

            TestCaseData(
                """"body":"{
                           \"enrollment\":{
                               \"reference\":\"49ed2e31-c172-11ec-badf-005056a147d6\"
                           },
                           \"tnc\":{
                               \"reference\":\"ad74bb5b-f9d6-4a0d-aa41-0512fea0d44e\",
                               \"timestamp\":1502960368146
                           }
                       }"
                """,
                TechField.Body (
                    jsonLog { 
                        Field "enrollment"
                            (
                                jsonLog {
                                    Field "reference" "49ed2e31-c172-11ec-badf-005056a147d6"
                                }
                            )

                        Field "tnc"
                            (
                                jsonLog {
                                    Field "reference" "ad74bb5b-f9d6-4a0d-aa41-0512fea0d44e"
                                    Field "timestamp" "1502960368146"
                                }
                            )
                    } 
                   )
            ).SetName("20 - Body. nested json")

            TestCaseData(
                """"body": "{ \"rabbitmq_node\": { } }"
                """,
                TechField.Body [ JsonLog.emptyJson "rabbitmq_node" ]
            ).SetName("20 - Body. empty object json field")


            TestCaseData(
                """"body": "{ 
                        \"certificates\":[ 
                            { \"usage\":\"CA\" } 
                        ]
                    }"
                """,
                TechField.Body (
                    jsonLog {
                        Field "certificates" [
                            (jsonLog { Field "usage" "CA" })
                        ]
                    }
                )
            ).SetName("20 - Body. with json array")
        }