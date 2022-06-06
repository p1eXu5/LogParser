namespace LogParser.Core.Tests.Factories

open Microsoft.Extensions.Logging
open NUnit.Framework
open System.Collections
open LogParser.Core.Dsl
open LogParser.Core.Types
open System.Net

type ParserTestCases () =

    static member InnerField : IEnumerable =
        seq {
            TestCaseData(
                """ \"customString\": \"foo\"}""",
                TechnoField.String ("customString", "foo")
            ).SetName("inner field parsing. String with quotes")

            TestCaseData(
                """ \"customString\": \"12345\"}""",
                TechnoField.String ("customString", "12345")
            ).SetName("inner field parsing. String int with quotes")

            TestCaseData(
                """ \"customString\": foo}""",
                TechnoField.String ("customString", "foo")
            ).SetName("inner field parsing. String with no quotes")

            TestCaseData(
                """ \"customString\":1502960368146}""",
                TechnoField.String ("customString", "1502960368146")
            ).SetName("inner field parsing. String numeric")

            TestCaseData(
                """ \"CardData.OpaqueInfo\":[\"The OpaqueInfo field is required.\"]}""",
                TechnoField.Array ("CardData.OpaqueInfo", ["The OpaqueInfo field is required."])
            ).SetName("inner field parsing. String array with qualified identifier")
        }


    static member FieldTests : IEnumerable =
        seq {
            // -------
            // message
            // -------
            yield!
                seq {
                    TestCaseData(
                        "\"message\": \"Returning next host: rabbitmq_node:5672\"",
                        TechnoField.Message "Returning next host: rabbitmq_node:5672"
                    ).SetName("field parsing. Message")

                    TestCaseData(
                        "\"message\": \"Error during removing token: ApiUnexpectedErrorResult { Message: \\\"Bad status code 404: \\\" }\"",
                        TechnoField.MessageParameterized (
                            "Error during removing",
                            [{
                                Key = "token"
                                TypeName = "ApiUnexpectedErrorResult"
                                Body =
                                    jsonLog {
                                        Field "Message" "Bad status code 404: " 
                                    } |> Log.fieldList
                            } |> MessageParameter.TypeJson])
                    ).SetName("field parsing. Message with curly brackets")

                    TestCaseData(
                        """ "message": 
                                "Returning next host: {
                                        \"rabbitmq_node\":5672
                                    }"
                        """,
                        TechnoField.MessageBoddied ("Returning next host:", (jsonLog { Field "rabbitmq_node" 5672 } |> Log.fieldList))
                    ).SetName("field parsing. MessageBoddied")

                    TestCaseData(
                        """ "message": 
                                "Returning next host: {
                                        \"rabbitmq_node\":5672
                                    } (status=Error)"
                        """,
                        TechnoField.MessageBoddiedWithPostfix (
                            "Returning next host:",
                            (jsonLog { Field "rabbitmq_node" 5672 } |> Log.fieldList),
                            " (status=Error)"
                        )
                    ).SetName("field parsing. MessageBoddied with postfix")

                    TestCaseData(
                        """ "message": 
                                "Returning next host, parameters: [( \"request\": DTO {
                                        rabbitmq_node:5672
                                    })] ."
                        """,
                        TechnoField.MessageParameterized (
                            "Returning next host, parameters:", 
                            [{
                                Key = "request"
                                TypeName = "DTO"
                                Body =
                                    jsonLog { Field "rabbitmq_node" 5672 } |> Log.fieldList
                            
                            } |> MessageParameter.TypeJson]
                        )
                    ).SetName("field parsing. MessageParameter")

                    TestCaseData(
                        """ "message": 
                                "Returning next host, parameters: 
                                    [
                                        ( \"request\": 123-123-123 )
                                    ] ."
                        """,
                        TechnoField.MessageParameterized (
                            "Returning next host, parameters:", 
                            [TechnoField.String ("request", "123-123-123") |> MessageParameter.TechnoField]
                        )
                    ).SetName("field parsing. MessageParameter - Field")

                    TestCaseData(
                        """ "message": 
                                "Returning next host, parameters: 
                                    [
                                        ( \"request\": 123-123-123 ),
                                        ( \"request\": 123-123-123 )
                                    ] ."
                        """,
                        TechnoField.MessageParameterized (
                            "Returning next host, parameters:", 
                            [
                                TechnoField.String ("request", "123-123-123") |> MessageParameter.TechnoField
                                TechnoField.String ("request", "123-123-123") |> MessageParameter.TechnoField
                            ]
                        )
                    ).SetName("field parsing. MessageParameter - two Field")

                    TestCaseData(
                        """ "message": 
                                "Incoming POST request to /abc/v1.0/deploy/1234abcd-abcd-11ff-bada-000011112222, parameters: 
                                [
                                    (\"tokenId\": 11b145b64-44gh-36wg-7rja-33654rerw34),
                                    (\"request\": RequestDto { 
                                        SessionId: cc0e8a3b-70e9-47dd-b6d0-07032ac93059,
                                        Id: \"78354-fgh4568-fgh57h\",
                                        TokenRequestorId: \"123456789\",
                                        PersoDataId: 654gf56-df4g-d354fg-dfgg, 
                                        DeviceTokenData: DeviceTokenData {
                                            Content: null,
                                            Encoding: BASE64_CIPHERED,
                                            PrisonId: \"12345\"
                                        }, 
                                        Renewal: False,
                                        TokenInfo: TokenInfo {
                                            ExpDate: \"1234\",
                                            Digits: \"4321\"
                                        }
                                    })
                                ]. "
                        """,
                        TechnoField.MessageParameterized (
                            "Incoming POST request to /abc/v1.0/deploy/1234abcd-abcd-11ff-bada-000011112222, parameters:", 
                            [
                                TechnoField.String ("tokenId", "11b145b64-44gh-36wg-7rja-33654rerw34") |> MessageParameter.TechnoField;
                                {
                                    Key = "request"
                                    TypeName = "RequestDto"
                                    Body =
                                        jsonLog {
                                            Field "SessionId" "cc0e8a3b-70e9-47dd-b6d0-07032ac93059"
                                            Field "Id" "78354-fgh4568-fgh57h"
                                            Field "TokenRequestorId" "123456789"
                                            Field "PersoDataId" "654gf56-df4g-d354fg-dfgg"
                                            Field "DeviceTokenData" "DeviceTokenData" (
                                                jsonLog {
                                                    Null "Content"
                                                    Field "Encoding" "BASE64_CIPHERED"
                                                    Field "PrisonId" "12345"
                                                }
                                            )
                                            Field "Renewal" false
                                            Field "TokenInfo" "TokenInfo" (
                                                jsonLog {
                                                    Field "ExpDate" "1234"
                                                    Field "Digits" "4321"
                                                }
                                            )
                                        } |> Log.fieldList
                                } |> MessageParameter.TypeJson
                            ]
                        )
                    ).SetName("field parsing. MessageParameter2")

                    TestCaseData(
                        """
                            "message": "foo {
                                \"rabbitmq_node\":{
                                    \"bar\": \"5672\",
                                    \"baz\": {
                                        \"port\": 1234
                                    }
                                }
                            }"
                        """,
                        jsonLog {
                            message
                                "foo"
                                (jsonLog {
                                    Field
                                        "rabbitmq_node"
                                        (jsonLog {
                                             Field "bar" "5672"
                                             Field 
                                                "baz"
                                                (jsonLog {
                                                    Field "port" 1234
                                                })
                                        })
                                })
                        } |> Log.fieldList |> List.head
                    ).SetName("field parsing. Message with nested json")

                    TestCaseData(
                        """
                            "message": "foo {
                                \"rabbitmq_node\":{
                                    \"bar\": \"5672\",
                                    \"baz\": {
                                        \"port\": 1234
                                    }
                                }
                            }"
                        """,
                        jsonLog {
                            message
                                "foo"
                                (jsonLog {
                                    Field
                                        "rabbitmq_node"
                                        (jsonLog {
                                             Field "bar" "5672"
                                             Field 
                                                "baz"
                                                (jsonLog {
                                                    Field "port" 1234
                                                })
                                        })
                                })
                        } |> Log.fieldList |> List.head
                    ).SetName("field parsing. Message with nested json")


                    TestCaseData(
                        """
                            "message":
                                "Response to POST /v8/rock/and/roll 200 {
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
                        } |> Log.fieldList |> List.head
                    ).SetName("field parsing. Message with complex nested json")


                }


            // -------------
            // simple values
            // -------------
            yield!
                seq {
                    TestCaseData(
                        "\"timestamp\": \"2022-04-14T11:49:52.912Z\"",
                        TechnoField.Timespan (Timespan.Value "2022-04-14T11:49:52.912Z")
                    ).SetName("field parsing. Timespan")

                    TestCaseData(
                        "\"level\": \"Debug\"",
                        TechnoField.Level LogLevel.Debug
                    ).SetName("field parsing. Level")

                    TestCaseData(
                        "\"level\": \"debuG\"",
                        TechnoField.Level LogLevel.Debug
                    ).SetName("field parsing. Level CI")

                    TestCaseData(
                        "\"method\": \"Post\"",
                        TechnoField.Method "Post"
                    ).SetName("field parsing. Method")

                    TestCaseData(
                        "\"statusCode\": \"200\"",
                        TechnoField.StatusCode HttpStatusCode.OK
                    ).SetName("field parsing. StatusCode")

                    TestCaseData(
                        "\"StatusCode\": \"200\"",
                        TechnoField.StatusCode HttpStatusCode.OK
                    ).SetName("field parsing. StatusCode CI")

                    TestCaseData(
                        "\"path\": \"MassTransit\"",
                        TechnoField.Path "MassTransit"
                    ).SetName("field parsing. Path")

                    TestCaseData(
                        "\"host\": \"MassTransit\"",
                        TechnoField.Host "MassTransit"
                    ).SetName("field parsing. Host")

                    TestCaseData(
                        "\"port\": 5672",
                        TechnoField.Port 5672
                    ).SetName("field parsing. Port")

                    TestCaseData(
                        "\"sourceContext\": \"MassTransit\"",
                        TechnoField.SourceContext "MassTransit"
                    ).SetName("field parsing. SourceContext")

                    TestCaseData(
                        "\"RequestId\": \"MassTransit\"",
                        TechnoField.RequestId "MassTransit"
                    ).SetName("field parsing. RequestId")

                    TestCaseData(
                        "\"RequeStPath\": \"MassTransit\"",
                        TechnoField.RequestPath "MassTransit"
                    ).SetName("field parsing. RequestPath")

                    TestCaseData(
                        "\"SpanId\": \"MassTransit\"",
                        TechnoField.SpanId "MassTransit"
                    ).SetName("field parsing. SpanId")

                    TestCaseData(
                        "\"TraceId\": \"MassTransit\"",
                        TechnoField.TraceId "MassTransit"
                    ).SetName("field parsing. TraceId")

                    TestCaseData(
                        "\"ParentId\": \"MassTransit\"",
                        TechnoField.ParentId "MassTransit"
                    ).SetName("field parsing. ParentId")

                    TestCaseData(
                        "\"ConnectionId\": \"MassTransit\"",
                        TechnoField.ConnectionId "MassTransit"
                    ).SetName("field parsing. ConnectionId")

                    TestCaseData(
                        "\"HierarchicalTraceId\": \"MassTransit\"",
                        TechnoField.HierarchicalTraceId "MassTransit"
                    ).SetName("field parsing. HierarchicalTraceId")
                }

            // ------------
            //     body
            // ------------
            yield!
                seq {
                    TestCaseData(
                        """
                            "body": "
                                {
                                    \"rabbitmq_node\": \"5672\"
                                }"
                        """,
                        TechnoField.Body (jsonLog { Field "rabbitmq_node" "5672" } |> Log.fieldList)
                    ).SetName("field parsing. Body")

                    TestCaseData(
                        """
                            "body":"
                                {
                                    \"rabbitmq_node\": \"5672\"
                                }"
                        """,
                        TechnoField.Body (jsonLog { Field "rabbitmq_node" "5672" } |> Log.fieldList)
                    ).SetName("field parsing. Body with nested json 2")

                    TestCaseData(
                        """
                            "body":"{
                                   \"enrollment\":{
                                       \"reference\":\"49ed2e31-c172-11ec-badf-005056a147d6\"
                                   },
                                   \"tnc\":{
                                       \"reference\":\"ad74bb5b-f9d6-4a0d-aa41-0512fea0d44e\",
                                       \"timestamp\":1502960368146
                                   }
                               }"
                        """,
                        TechnoField.Body (
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
                            |> Log.fieldList)
                    ).SetName("field parsing. Body with array")
                }


            // ------------
            // primitives
            // ------------
            yield!
                seq {
                    TestCaseData(
                        "\"customString\": \"foo\"",
                        TechnoField.String ("customString", "foo")
                    ).SetName("field parsing. String")

                    TestCaseData(
                        "\"customInt\": 123",
                        TechnoField.Int ("customInt", 123)
                    ).SetName("field parsing. Int")

                    TestCaseData(
                        "\"customBool\": true",
                        TechnoField.Bool ("customBool", true)
                    ).SetName("field parsing. Bool: true")

                    TestCaseData(
                        "\"customBool\": false",
                        TechnoField.Bool ("customBool", false)
                    ).SetName("field parsing. Bool: false")

                    TestCaseData(
                        """
                            "scope":["HTTP POST http://gate_be:5000/deploy/push"]
                        """,
                        TechnoField.Array ("scope", ["HTTP POST http://gate_be:5000/deploy/push"])
                    ).SetName("field parsing. Array")

                    TestCaseData(
                        """
                            "scope":[3]
                        """,
                        TechnoField.ArrayInt ("scope", [3])
                    ).SetName("field parsing. ArrayInt")


                    TestCaseData(
                        """
                            "foo": null
                        """,
                        TechnoField.Null "foo"
                    ).SetName("field parsing. Null")
                }


            // -------------
            //     json     
            // -------------
            yield!
                seq {
                    TestCaseData(
                        """
                            "foo": "{
                                \"rabbitmq_node\":5672
                            }"
                        """,
                        TechnoField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 } |> Log.fieldList))
                    ).SetName("field parsing. Json")

                    TestCaseData(
                        """
                            "foo": {
                                "rabbitmq_node":5672
                            }
                        """,
                        TechnoField.Json ("foo", (jsonLog { Field "rabbitmq_node" 5672 } |> Log.fieldList))
                    ).SetName("field parsing. Json not wrapped")

                    TestCaseData(
                        """
                            "foo": "{
                                \"rabbitmq_node\":{
                                    \"bar\": \"5672\"
                                }
                            }"
                        """,
                        jsonLog {
                            Field
                                "foo"
                                (jsonLog {
                                    Field
                                        "rabbitmq_node"
                                        (jsonLog {
                                             Field "bar" "5672"
                                        })
                                })
                        } |> Log.fieldList |> List.head
                    ).SetName("field parsing. Json nested")
                }
        }

    static member LogTests : IEnumerable =
        seq {
            TestCaseData(
                """{"timestamp":"2022-04-14T11:49:52.912Z","message":"Returning next host: rabbitmq_node:5672","level":"Debug","host":"rabbitmq_node","port":5672,"sourceContext":"MassTransit"}""",

                jsonLog {
                    timestamp "2022-04-14T11:49:52.912Z"
                    message "Returning next host: rabbitmq_node:5672"
                    level "Debug"
                    host "rabbitmq_node"
                    port 5672
                    sourceContext "MassTransit"
                } |> Log.TechnoLog
                
            ).SetName("log parsing. short simple log techno message")

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
                            }"
                    }
                """,
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
                } |> Log.TechnoLog
            ).SetName("log parsing. with buddied message")

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
                    } |> Log.TechnoLog
                )
            ).SetName("log parsing. complex log techno message")

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
                } |> Log.TechnoLog
            ).SetName("log parsing. complex log techno message 2")

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
                        "uri":"http://gate_be:5000/deploy/push",
                        "parentId":"2c69b1b0b885974e",
                        "sourceContext":"Common.Http.Logging.Microsoft.LoggingFilter",
                        "spanId":"126d5e554ae9ec4c",
                        "externalSystemName":"Narnia",
                        "requestId":"0HMGU1M6CAUUV:00000001",
                        "scope":["HTTP POST http://gate_be:5000/deploy/push"],
                        "connectionId":"0HMGU1M6CAUUV",
                        "actionId":"d2ba3018-c681-4abe-a7c7-88bf43c38534",
                        "requestPath":"/pmp/v1.0/deploy/41b2dad7-bc75-11ec-badf-005056a147d6",
                        "timestamp":"2022-04-15T04:33:54.621Z",
                        "statusCode":404,
                        "actionName":"Techno.Xpay.TSM.Gateway.Narnia.Controllers.Pmp.TsmDeployController.tokenAsync (Techno.Xpay.TSM.Gateway.Narnia)"
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
                    Field "uri" "http://gate_be:5000/deploy/push"
                    parentId "2c69b1b0b885974e"
                    sourceContext "Common.Http.Logging.Microsoft.LoggingFilter"
                    spanId "126d5e554ae9ec4c"
                    Field "externalSystemName" "Narnia"
                    requestId "0HMGU1M6CAUUV:00000001"
                    Field "scope" ["HTTP POST http://gate_be:5000/deploy/push"]
                    connectionId "0HMGU1M6CAUUV"
                    Field "actionId" "d2ba3018-c681-4abe-a7c7-88bf43c38534"
                    requestPath "/pmp/v1.0/deploy/41b2dad7-bc75-11ec-badf-005056a147d6"
                    timestamp "2022-04-15T04:33:54.621Z"
                    statusCode HttpStatusCode.NotFound
                    Field "actionName" "Techno.Xpay.TSM.Gateway.Narnia.Controllers.Pmp.TsmDeployController.tokenAsync (Techno.Xpay.TSM.Gateway.Narnia)"
                } |> Log.TechnoLog
            ).SetName("log parsing. complex log with array")

            TestCaseData(
                """
                    {
                        "timestamp":"2021-04-28T13:14:45.585Z",
                        "message":"Incoming POST request to /v8/enroll/enroll, parameters: [
                                (
                                     \"request\": EnrollCardLocalRequestDto { 
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
                        ({
                            Key = "request"
                            TypeName = "EnrollCardLocalRequestDto"
                            Body =
                                jsonLog {
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
                                } |> Log.fieldList
                        })
                } |> Log.TechnoLog
            ).SetName("log parsing. log with message parameterized")

            TestCaseData(
                """
                    {
                        "timestamp":"2021-04-28T13:14:45.585Z",
                        "message":"Incoming POST request to /v8/enroll/enroll, parameters: [
                                (
                                     \"request\": EnrollCardLocalRequestDto { 
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
                        ({
                            Key = "request"
                            TypeName = "EnrollCardLocalRequestDto"
                            Body =
                                jsonLog {
                                    Field "RawWspData"
                                        (jsonLog {
                                            Field "deviceId" "98DFGJINHD989I5OT34095"
                                        })
                                    Field "RegDateTime" "04/28/2021 11:24:57"
                                } |> Log.fieldList
                        })
                } |> Log.TechnoLog
            ).SetName("log parsing. log with message parameterized with json")

            // TODO: will be fixed when '\u00a0' whitespace will be added to parser
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
                } |> Log.TechnoLog
                
            ).SetName("log parsing. body with simple json field")

            // TODO: will be fixed when '\u00a0' whitespace will be added to parser
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
                } |> Log.TechnoLog
                
            ).SetName("log parsing. body with nested json")
        }
