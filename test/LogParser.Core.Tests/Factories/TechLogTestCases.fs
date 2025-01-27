namespace LogParser.Core.Tests.Factories

open System.Collections
open System.Net
open Microsoft.Extensions.Logging

open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type TechLogTestCases () =

    static member TechLogs : IEnumerable =
        seq {

            TestCaseData(
                "{\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"}",
                jsonLog {
                    Field "requestBody" (jsonLog { Field "bankId" "1234" })
                }
                |> Log.fromTechJson
            ).SetName("50 - log parsing. {\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"}")

            TestCaseData(
                """{"timestamp":"2022-04-14T11:49:52.912Z","message":"Returning next host: rabbitmq_node:5672","level":"Debug","host":"rabbitmq_node","port":5672,"sourceContext":"MassTransit"}""",

                jsonLog {
                    timestamp "2022-04-14T11:49:52.912Z"
                    message "Returning next host: rabbitmq_node:5672"
                    level "Debug"
                    host "rabbitmq_node"
                    port 5672
                    sourceContext "MassTransit"
                } 
                |> Log.fromTechJson
                
            ).SetName("50 - log parsing. short simple log tech message")

            TestCaseData(
                """{"timestamp":"2022-04-14T11:49:52.912Z","message":"Returning next host: rabbitmq_node:5672","level":"Debug","host":"rabbitmq_node","port":5672,"sourceContext":"MassTransit"}   """,

                jsonLog {
                    timestamp "2022-04-14T11:49:52.912Z"
                    message "Returning next host: rabbitmq_node:5672"
                    level "Debug"
                    host "rabbitmq_node"
                    port 5672
                    sourceContext "MassTransit"
                } 
                |> Log.fromTechJson
                
            ).SetName("50 - log parsing. short simple log tech message with trailing spaces")

            TestCaseData(
                """
                    {
                        "message":
                            "Response to POST /v8/rock/and/roll 200 
                                {
                                    \"bin\": \"123456\",
                                    \"productId\":\"prd\",
                                    \"metadata\":{
                                        \"lastDigits\":\"8945\",
                                        \"exp\":\"1234\",
                                        \"expFormat\":\"MMYY\"
                                },
                                \"cardId\":\"a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9\",
                                \"issuerId\":\"482386d9-3507-4bef-a227-7bcdb01f1e70\",
                                \"designId\":\"DF09987BC2F\"
                            } (status=Error)"
                    }
                """,
                jsonLog {
                    message
                        "Response to POST /v8/rock/and/roll 200"
                        (jsonLog {
                            Field "bin" "123456"
                            Field "productId" "prd"
                            Field 
                                "metadata"
                                (jsonLog {
                                    Field "lastDigits" "8945"
                                    Field "exp" "1234"
                                    Field "expFormat" "MMYY"
                                })
                            
                            Field "cardId" "a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9"
                            Field "issuerId" "482386d9-3507-4bef-a227-7bcdb01f1e70"
                            Field "designId" "DF09987BC2F"
                        })
                        "(status=Error)"
                } 
                |> Log.fromTechJson
            ).SetName("50 - log parsing. with buddied message with postfix")

            TestCaseData(
                """
                    {
                        "message":
                            "Response to POST /v8/rock/and/roll 200 
                                {
                                    \"bin\": \"123456\",
                                    \"productId\":\"prd\",
                                    \"metadata\":{
                                        \"lastDigits\":\"8945\",
                                        \"exp\":\"1234\",
                                        \"expFormat\":\"MMYY\"
                                },
                                \"cardId\":\"a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9\",
                                \"issuerId\":\"482386d9-3507-4bef-a227-7bcdb01f1e70\",
                                \"designId\":\"DF09987BC2F\"
                            }"
                    }
                """,
                jsonLog {
                    message
                        "Response to POST /v8/rock/and/roll 200"
                        (jsonLog {
                            Field "bin" "123456"
                            Field "productId" "prd"
                            Field 
                                "metadata"
                                (jsonLog {
                                    Field "lastDigits" "8945"
                                    Field "exp" "1234"
                                    Field "expFormat" "MMYY"
                                })
                            
                            Field "cardId" "a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9"
                            Field "issuerId" "482386d9-3507-4bef-a227-7bcdb01f1e70"
                            Field "designId" "DF09987BC2F"
                        }) 
                } |> Log.fromTechJson
            ).SetName("50 - log parsing. with buddied message")

            TestCaseData(
                """
                    {
                        "timestamp":"2022-04-14T13:25:31.115Z",
                        "message":
                            "Response to POST /v8/rock/and/roll 200 
                                {
                                    \"bin\": \"123456\",
                                    \"productId\":\"prd\",
                                    \"metadata\":{
                                        \"lastDigits\":\"8945\",
                                        \"exp\":\"1234\",
                                        \"expFormat\":\"MMYY\"
                                    },
                                    \"cardId\":\"a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9\",
                                    \"issuerId\":\"482386d9-3507-4bef-a227-7bcdb01f1e70\",
                                    \"designId\":\"DF09987BC2F\"
                                }",
                        "level":"Trace",
                        "method":"POST",
                        "path":"/v8/rock/and/roll",
                        "statusCode":200,
                        "body":"
                            {
                                \"bin\":\"123456\",
                                \"productId\":\"prd\",
                                \"metadata\":{
                                    \"lastDigits\":\"8945\",
                                    \"exp\":\"1234\",
                                    \"expFormat\":\"MMYY\"
                                },
                                \"cardId\":\"a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9\",
                                \"issuerId\":\"482386d9-3507-4bef-a227-7bcdb01f1e70\",
                                \"designId\":\"DF09987BC2F\"
                            }",
                        "sourceContext":"Common.Mvc.Middlewares.ResponseLoggingMiddleware",
                        "requestId":"0HMGU1JHPH9FR:00000001",
                        "requestPath":"/v8/rock/and/roll",
                        "spanId":"08b4e925fa92aa40",
                        "traceId":"87ac8ea8142e664f998a1d858052e1eb",
                        "parentId":"e3c9175453cf5040",
                        "connectionId":"0HMGU1JHPH9FR",
                        "hierarchicalTraceId":"|87ac8ea8142e664f998a1d858052e1eb.555.08b4e925_"
                    }
                """,
                (
                    jsonLog {
                        timestamp "2022-04-14T13:25:31.115Z"
                        message
                            "Response to POST /v8/rock/and/roll 200"
                            (jsonLog {
                                Field "bin" "123456"
                                Field "productId" "prd"
                                Field 
                                    "metadata"
                                    (jsonLog {
                                        Field "lastDigits" "8945"
                                        Field "exp" "1234"
                                        Field "expFormat" "MMYY"
                                    })
                                
                                Field "cardId" "a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9"
                                Field "issuerId" "482386d9-3507-4bef-a227-7bcdb01f1e70"
                                Field "designId" "DF09987BC2F"
                            })
                        level LogLevel.Trace
                        method "POST"
                        path "/v8/rock/and/roll"
                        statusCode HttpStatusCode.OK
                        body (
                            jsonLog {
                                Field "bin" "123456"
                                Field "productId" "prd"
                                Field 
                                    "metadata"
                                    (jsonLog {
                                        Field "lastDigits" "8945"
                                        Field "exp" "1234"
                                        Field "expFormat" "MMYY"
                                    })
                                
                                Field "cardId" "a3ca84ba-6d18-4cd4-a5d4-4dd097ad3eb9"
                                Field "issuerId" "482386d9-3507-4bef-a227-7bcdb01f1e70"
                                Field "designId" "DF09987BC2F"
                            }
                        )
                        sourceContext "Common.Mvc.Middlewares.ResponseLoggingMiddleware"
                        requestId "0HMGU1JHPH9FR:00000001" 
                        requestPath "/v8/rock/and/roll" 
                        spanId "08b4e925fa92aa40" 
                        traceId "87ac8ea8142e664f998a1d858052e1eb" 
                        parentId "e3c9175453cf5040" 
                        connectionId "0HMGU1JHPH9FR" 
                        hierarchicalTraceId "|87ac8ea8142e664f998a1d858052e1eb.555.08b4e925_"
                    } |> Log.fromTechJson
                )
            ).SetName("50 - log parsing. complex log tech message")

            TestCaseData(
                """ {
                    "timestamp":"2022-04-14T11:50:50.466Z",
                    "message":"Request to POST /ws/t6/card/keks  {
                        \"pan\":\"123456789\",
                        \"expDate\":\"1234\"
                    }",
                    "level":"Trace",
                    "method":"POST",
                    "path":"/ws/t6/card/keks",
                    "query":"",
                    "body":"{
                        \"pan\":\"123456789\",
                        \"expDate\":\"1234\"
                    }",
                    "sourceContext":"Host.Middlewares.RequestLoggingMiddleware",
                    "requestId":"0HMGU1JEEI1D7:00000001",
                    "requestPath":"/ws/t6/card/keks",
                    "spanId":"ecd5c55f5dde574d",
                    "traceId":"f1907304a203694c81345dfa93303aaf",
                    "parentId":"aa0c97c70c85ab43",
                    "connectionId":"0HMGU1JEEI1D7"}
                """,
                jsonLog {
                    timestamp "2022-04-14T11:50:50.466Z"
                    message
                        "Request to POST /ws/t6/card/keks"
                        (jsonLog {
                            Field "pan" "123456789"
                            Field "expDate" "1234"
                        })
                    level LogLevel.Trace
                    method "POST"
                    path "/ws/t6/card/keks"
                    Field "query" ""
                    body
                        (jsonLog {
                            Field "pan" "123456789"
                            Field "expDate" "1234"
                        })
                    sourceContext "Host.Middlewares.RequestLoggingMiddleware"
                    requestId "0HMGU1JEEI1D7:00000001"
                    requestPath "/ws/t6/card/keks"
                    spanId "ecd5c55f5dde574d"
                    traceId "f1907304a203694c81345dfa93303aaf"
                    parentId "aa0c97c70c85ab43"
                    connectionId "0HMGU1JEEI1D7"
                } |> Log.fromTechJson
            ).SetName("50 - log parsing. complex log tech message 2")

            TestCaseData(
                """
                    {
                        "traceId":"20263e600f3111219fe33ae838f1fe93",
                        "eventId":"ReceivingResponse",
                        "level":"Information",
                        "hierarchicalTraceId":"|20263e600f3111219fe33ae838f1fe93.828.e99dae60_726.67aab6a0_679.ba0f0ef3_618.1471467c_964.768354f0_918.8a54924b_892.126d5e55_",
                        "eventType":"token",
                        "httpMethod":"POST",
                        "message":"Response was received from Narnia. Status Code 404",
                        "uri":"http://gate_app:5000/deploy/push",
                        "parentId":"2c69b1b0b885974e",
                        "sourceContext":"Common.Http.Logging.Microsoft.LoggingFilter",
                        "spanId":"126d5e554ae9ec4c",
                        "externalSystemName":"Narnia",
                        "requestId":"0HMGU1M6CAUUV:00000001",
                        "scope":["HTTP POST http://gate_app:5000/deploy/push"],
                        "connectionId":"0HMGU1M6CAUUV",
                        "actionId":"d2ba3018-c681-4abe-a7c7-88bf43c38534",
                        "requestPath":"/pmp/v1.0/deploy/41b2dad7-bc75-11ec-badf-005056a147d6",
                        "timestamp":"2022-04-15T04:33:54.621Z",
                        "statusCode":404,
                        "actionName":"Metal.Pay.ABC.Gateway.Narnia.Controllers.Pmp.AbcDeployController.tokenAsync (Metal.Pay.ABC.Gateway.Narnia)"
                    }
                """,
                jsonLog {
                    traceId "20263e600f3111219fe33ae838f1fe93"
                    eventId "ReceivingResponse"
                    level "Information"
                    hierarchicalTraceId "|20263e600f3111219fe33ae838f1fe93.828.e99dae60_726.67aab6a0_679.ba0f0ef3_618.1471467c_964.768354f0_918.8a54924b_892.126d5e55_"
                    Field "eventType" "token"
                    Field "httpMethod" "POST"
                    message "Response was received from Narnia. Status Code 404"
                    Field "uri" "http://gate_app:5000/deploy/push"
                    parentId "2c69b1b0b885974e"
                    sourceContext "Common.Http.Logging.Microsoft.LoggingFilter"
                    spanId "126d5e554ae9ec4c"
                    Field "externalSystemName" "Narnia"
                    requestId "0HMGU1M6CAUUV:00000001"
                    Field "scope" ["HTTP POST http://gate_app:5000/deploy/push"]
                    connectionId "0HMGU1M6CAUUV"
                    Field "actionId" "d2ba3018-c681-4abe-a7c7-88bf43c38534"
                    requestPath "/pmp/v1.0/deploy/41b2dad7-bc75-11ec-badf-005056a147d6"
                    timestamp "2022-04-15T04:33:54.621Z"
                    statusCode HttpStatusCode.NotFound
                    Field "actionName" "Metal.Pay.ABC.Gateway.Narnia.Controllers.Pmp.AbcDeployController.tokenAsync (Metal.Pay.ABC.Gateway.Narnia)"
                } |> Log.fromTechJson
            ).SetName("50 - log parsing. complex log with array")

            TestCaseData(
                """
                    {
                        "timestamp":"2021-04-28T13:14:45.585Z",
                        "message":"Incoming POST request to /v8/enroll/enroll, parameters: [
                                (
                                     \"request\": RollCardLocalRequestDto { 
                                         TokenRequestorId: \"16700030001\", 
                                         IssuerId: \"12345\", 
                                         EnrollmentId: \"15e6af0c-59c6-481e-a7db-d3311659565d\", 
                                         WalletId: \"15e6af0c-59c6-481e-a7db-d3311659565d\", 
                                         DeviceId: \"15e6af0c-59c6-481e-a7db-d3311659565d\", 
                                         UserId: \"$8dfkjngdf908dfgkjdfg0dufgon\", 
                                         DeviceMetadata: DeviceMetadata { 
                                             Scoring: DeviceMetadataScoring { 
                                                 DeviceScore: 5, 
                                                 AccountScore: 5, 
                                                 CardNewlyAdded: True, 
                                                 LevelOfTrust: Yellow,
                                                 LevelOfTrustStandardVersion: \"01\", 
                                                 CofTenureWeeks: 321, 
                                                 DeviceCountry: \"BR\", 
                                                 ReasonCodes: null
                                             }
                                         }
                                     }
                                )
                            ]"
                    }
                """,
                jsonLog {
                    timestamp "2021-04-28T13:14:45.585Z"
                    message "Incoming POST request to /v8/enroll/enroll, parameters:"
                        (jsonLog {
                            Field "request" "RollCardLocalRequestDto"
                                (jsonLog {
                                    Field "TokenRequestorId" "16700030001"
                                    Field "IssuerId" "12345"
                                    Field "EnrollmentId" "15e6af0c-59c6-481e-a7db-d3311659565d"
                                    Field "WalletId" "15e6af0c-59c6-481e-a7db-d3311659565d"
                                    Field "DeviceId" "15e6af0c-59c6-481e-a7db-d3311659565d"
                                    Field "UserId" "$8dfkjngdf908dfgkjdfg0dufgon"
                                    Field "DeviceMetadata" "DeviceMetadata"
                                        (jsonLog {
                                            Field "Scoring" "DeviceMetadataScoring"
                                                (jsonLog {
                                                    Field "DeviceScore" 5
                                                    Field "AccountScore" 5
                                                    Field "CardNewlyAdded" true
                                                    Field "LevelOfTrust" "Yellow"
                                                    Field "LevelOfTrustStandardVersion" "01"
                                                    Field "CofTenureWeeks" 321
                                                    Field "DeviceCountry" "BR"
                                                    Null "ReasonCodes"
                                                })
                                        })
                                })
                        })
                } |> Log.fromTechJson
            ).SetName("50 - log parsing. log with message parameterized")

            TestCaseData(
                """
                    {
                        "timestamp":"2021-04-28T13:14:45.585Z",
                        "message":"Incoming POST request to /v8/enroll/enroll, parameters: [
                                (
                                     \"request\": RollCardLocalRequestDto { 
                                         RawWspData: { 
                                            deviceId: \"98DFGJINHD989I5OT34095\"
                                         },
                                         RegDateTime: 04/28/2021 11:24:57
                                     }
                                )
                            ]"
                    }
                """,
                jsonLog {
                    timestamp "2021-04-28T13:14:45.585Z"
                    message "Incoming POST request to /v8/enroll/enroll, parameters:"
                        (jsonLog {
                            Field "request" "RollCardLocalRequestDto"
                                (jsonLog {
                                    Field "RawWspData"
                                        (jsonLog {
                                            Field "deviceId" "98DFGJINHD989I5OT34095"
                                        })
                                    Field "RegDateTime" "04/28/2021 11:24:57"
                                })
                        })
                } |> Log.fromTechJson
            ).SetName("50 - log parsing. log with message parameterized with json")

            TestCaseData(
                """
                {
                    "body":"{\"recipient\": \"sdf\"}"
                }
                """,

                jsonLog {
                    body (
                        jsonLog {
                            Field "recipient" "sdf"
                        }
                    )
                } |> Log.fromTechJson
                
            ).SetName("50 - log parsing. body with simple json field")

            TestCaseData(
                """
                {
                    "body":"{
                        \"requestInfo\": { 
                            \"id\": \"14d0b39f-06d3-41d4-a9ec-3242408da069\", 
                            \"recipient\": \"12345\", 
                            \"sender\": \"APAY\", 
                            \"ts\": \"2022-6-3T06:49:16.655Z\", 
                            \"exchangeId\": \"a2ae9efe-1efc-405e-8236-cbd58b329a69\" 
                        }
                    }"
                }
                """,

                jsonLog {
                    body (
                        jsonLog {
                            Field "requestInfo" (
                                jsonLog {
                                    Field "id" "14d0b39f-06d3-41d4-a9ec-3242408da069"
                                    Field "recipient" "12345"
                                    Field "sender" "APAY"
                                    Field "ts" "2022-6-3T06:49:16.655Z"
                                    Field "exchangeId" "a2ae9efe-1efc-405e-8236-cbd58b329a69"
                                }
                            )
                        }
                    )
                } |> Log.fromTechJson
                
            ).SetName("50 - log parsing. body with nested json")

            TestCaseData(
                """
                {
                    "hierarchicalTraceId":"|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
                }
                """,

                jsonLog {
                    hierarchicalTraceId "|835674ebb32a89144396ba52f55405ef.457.c49360ab_"
                } |> Log.fromTechJson
                
            ).SetName("50 - log parsing. single hierarchicalTraceId")

            let tripleQuotes = "\"\"\""

            TestCaseData(
                $"
                {{
                    \"fullMessage\": {tripleQuotes}{{\"timestamp\": \"2022-07-15T04:02:47.002Z\"}}{tripleQuotes}
                }}
                ",

                jsonLog {
                    Field "fullMessage"
                        (jsonLog {
                            timestamp "2022-07-15T04:02:47.002Z"
                        })
                    timestamp "2022-07-15T04:02:47.002Z"
                } |> Log.fromTechJson
                
            ).SetName("50 - log parsing. kibana full message with timestamp")
        }
