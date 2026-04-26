namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type MessageBodiedCases() =

    /// 43
    static member MessageBodied : IEnumerable =
        seq {
            TestCaseData(
                """"message": 
                        "Returning next host: {
                                \"rabbitmq_node\":5672
                            }"
                """,
                TechJsonField.MessageBoddied ("Returning next host:", jsonLog { Field "rabbitmq_node" 5672 })
            ).SetName("MessageBodied with simple json")


            TestCaseData(
                """"message": 
                        "Returning next EndPoint=IPEndPoint {
                                rabbitmq_node: 5672
                            }"
                """,
                TechJsonField.MessageBoddied ("Returning next EndPoint=IPEndPoint", jsonLog { Field "rabbitmq_node" 5672 })
            ).SetName("MessageBodied with equal sign in header and simple json")


            TestCaseData(
                """"message": "foo {
                        \"rabbitmq_node\":{
                            \"bar\": \"5672\",
                            \"baz\": {
                                \"port\": 1234
                            }
                        }
                    }"
                """,
                TechJsonField.MessageBoddied (
                    "foo",
                    (jsonLog {
                        Field
                            "rabbitmq_node"
                            (jsonLog {
                                Field "bar" "5672"
                                Field 
                                    "baz"
                                    (jsonLog {
                                        port 1234
                                    })
                            })
                    })
                )
            ).SetName("MessageBodied with nested json")


            TestCaseData(
                """"message":
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
                TechJsonField.MessageBoddied (
                        "Response to POST /v8/rock/and/roll 200",
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
                )
            ).SetName("MessageBodied with complex nested json")

            TestCaseData(
                "\"message\": \"Error during removing token: ApiUnexpectedErrorResult { Message: \\\"Bad status code 404: \\\" }\"",
                TechJsonField.MessageBoddied (
                    "Error during removing token: ApiUnexpectedErrorResult",
                    (jsonLog {
                        message "Bad status code 404: "
                    })
                )
            ).SetName("MessageBodied with curly brackets")

            TestCaseData(
                """"message": "Returning next host, parameters: [ ( \"request\": \"123-123-123\" ) ]"
                """,
                TechJsonField.MessageBoddied (
                    "Returning next host, parameters:", 
                    jsonLog {
                        Field "request" "123-123-123"
                    }
                )
            ).SetName("MessageBodied with simple list")
        }