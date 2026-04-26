namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type BodyCases() =

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