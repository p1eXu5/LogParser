namespace LogParser.Core.Tests.Factories

open System.Collections
open NUnit.Framework
open LogParser.Core.Types
open LogParser.Core.Dsl


type MessageTestCases() =

    /// 41
    static member MessageJsonAnnotatedList : IEnumerable =
        seq {
            TestCaseData(
                "[ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] ",
                TechField.JsonAnnotated {
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
                TechField.JsonAnnotated {
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
                TechField.JsonAnnotated {
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

    /// 42
    static member MessageFactory(n: int) : IEnumerable =
        let testCaseName = sprintf "%i - %s" n
        seq {
            TestCaseData(
                "\"message\": \"Returning next host: rabbitmq_node:5672\"",
                TechField.Message "Returning next host: rabbitmq_node:5672"
            ).SetName("simple `message` field" |> testCaseName)

            TestCaseData(
                "Message: \\\"Bad status code 404: \\\"",
                TechField.Message "Bad status code 404: "
            ).SetName("`Message` key with no quotes" |> testCaseName)

            TestCaseData(
                "\"message\": \"!!!!!!!!!!!!!!!!\"",
                TechField.Message "!!!!!!!!!!!!!!!!"
            ).SetName("`message` contains only exclamation marks" |> testCaseName)
        }

    static member Message : IEnumerable = MessageTestCases.MessageFactory(1)
    static member Message2 : IEnumerable = MessageTestCases.MessageFactory(2)

    /// 43
    static member MessageBodied : IEnumerable =
        seq {
            TestCaseData(
                """"message": 
                        "Returning next host: {
                                \"rabbitmq_node\":5672
                            }"
                """,
                TechField.MessageBoddied ("Returning next host:", jsonLog { Field "rabbitmq_node" 5672 })
            ).SetName("MessageBodied with simple json")


            TestCaseData(
                """"message": 
                        "Returning next EndPoint=IPEndPoint {
                                rabbitmq_node: 5672
                            }"
                """,
                TechField.MessageBoddied ("Returning next EndPoint=IPEndPoint", jsonLog { Field "rabbitmq_node" 5672 })
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
                TechField.MessageBoddied (
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
                TechField.MessageBoddied (
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
                TechField.MessageBoddied (
                    "Error during removing token: ApiUnexpectedErrorResult",
                    (jsonLog {
                        message "Bad status code 404: "
                    })
                )
            ).SetName("MessageBodied with curly brackets")

            TestCaseData(
                """"message": "Returning next host, parameters: [ ( \"request\": \"123-123-123\" ) ]"
                """,
                TechField.MessageBoddied (
                    "Returning next host, parameters:", 
                    jsonLog {
                        Field "request" "123-123-123"
                    }
                )
            ).SetName("MessageBodied with simple list")
        }

    /// 44
    static member MessageBodiedWithPostfix : IEnumerable =
        seq {
            TestCaseData(
                """"message": 
                        "Returning next host: {
                                \"rabbitmq_node\":5672
                            } (status=Error)"
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Returning next host:",
                    jsonLog { Field "rabbitmq_node" 5672 },
                    "(status=Error)"
                )
            ).SetName("MessageBodiedWithPostfix with simple json")

            TestCaseData(
                """"message": 
                        "Returning next host, parameters: 
                            [
                                ( \"request\": 123-123-123 )
                            ] ."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Returning next host, parameters:", 
                    jsonLog {
                        Field "request" "123-123-123"
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with simple list")

            TestCaseData(
                "\"message\": \"foo parameters: [ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] . \" ",
                TechField.MessageBoddiedWithPostfix (
                    "foo parameters:",
                    jsonLog { Field "request" "DTO" (jsonLog { Field "rabbitmq_node" 5672}) },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with double escaped quotes list")

            TestCaseData(
                "\"message\": \"foo parameters: [ ( \"request\": DTO { rabbitmq_node:5672 } ) ] . \" ",
                TechField.MessageBoddiedWithPostfix (
                    "foo parameters:",
                    jsonLog { Field "request" "DTO" (jsonLog { Field "rabbitmq_node" 5672}) },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with double quotes list")

            TestCaseData(
                "\"message\": \"foo parameters: DTO { rabbitmq_node:5672 }. \" ",
                TechField.MessageBoddiedWithPostfix (
                    "foo parameters: DTO",
                    jsonLog { Field "rabbitmq_node" 5672},
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with no quotes")

            TestCaseData(
                """"message": "foo parameters: [( 
                    \"request\": DTO { 
                        rabbitmq_node:5672,
                        DeviceMetadata: DeviceMetadata { 
                            Scoring: DeviceMetadataScoring { 
                              DeviceCountry: \"BR\"
                            }
                        }
                    })] . " 
                """,
                TechField.MessageBoddiedWithPostfix (
                    "foo parameters:",
                    jsonLog {
                        Field "request" "DTO"
                            (jsonLog {
                                Field "rabbitmq_node" 5672
                                Field "DeviceMetadata" "DeviceMetadata"
                                    (jsonLog {
                                        Field "Scoring" "DeviceMetadataScoring"
                                            (jsonLog {
                                                Field "DeviceCountry" "BR"
                                            })
                                    })
                            })
                    }
                    ,
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with nested list")


            TestCaseData(
                """"message": 
                        "Returning next host, parameters: [( \"request\": DTO {
                                rabbitmq_node:5672
                            })] ."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Returning next host, parameters:",
                    jsonLog {
                        Field "request" "DTO"
                            (jsonLog { Field "rabbitmq_node" 5672 })
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with TypeAnnotatedJson")


            TestCaseData(
                """"message": 
                        "Incoming POST, parameters: 
                            [
                                (\"checkRequest\": [CipheredInformation { WalletRefId: \"1234\" } ] )
                            ]."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Incoming POST, parameters:",
                    jsonLog {
                        Field "checkRequest" [
                            { Key = ""; Annotation = "CipheredInformation"; Body = (jsonLog { Field "WalletRefId" "1234"}) }
                        ]
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with array of json field")

            TestCaseData(
                """"message": 
                        "Incoming POST, parameters: 
                            [
                                (\"checkRequest\": CheckRequestDto { 
                                    CipheredInformations: [CipheredInformation { WalletRefId: \"1234\" } ] 
                                })
                            ]."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Incoming POST, parameters:",
                    jsonLog {
                        Field "checkRequest" "CheckRequestDto"
                            (jsonLog { 
                                Field "CipheredInformations" [
                                    { Key = ""; Annotation = "CipheredInformation"; Body = (jsonLog { Field "WalletRefId" "1234"}) }
                            ] })
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with nested array of json field")


            TestCaseData(
                """"message": 
                        "Incoming POST, parameters: 
                        [
                            (\"checkRequest\": CheckRequestDto { 
                                  CipheredInformations: [CipheredInformation { WalletId: \"1234\", CipheredData: \"ew1kxazNWIg0KfQ==\" }],
                                  CipheredInformationFormat: CipheredDataFormat { 
                                      Ciphering: ContentCiphering { Reference: \"ABC.01\", Algo: AESPAD30, Ycv: null } 
                                  },
                                  Id: \"15700010001\",
                                  Info: MessageInfo { Id: 14d0b39f-06d3-41d4-a9ec-3242408da069} 
                            })
                        ]."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Incoming POST, parameters:",
                    jsonLog {
                        Field "checkRequest" "CheckRequestDto"
                            (jsonLog {
                                Field "CipheredInformations"
                                    [
                                        {
                                            Key = "";
                                            Annotation = "CipheredInformation";
                                            Body =
                                                (jsonLog { 
                                                    Field "WalletId" "1234"
                                                    Field "CipheredData" "ew1kxazNWIg0KfQ=="
                                                })
                                        }
                                    ]

                                Field "CipheredInformationFormat" "CipheredDataFormat"
                                    (jsonLog {
                                        Field "Ciphering" "ContentCiphering"
                                            (jsonLog {
                                                Field "Reference" "ABC.01"
                                                Field "Algo" "AESPAD30"
                                                Null "Ycv"
                                            })
                                    })

                                Field "Id" "15700010001"
                                Field "Info" "MessageInfo"
                                    (jsonLog {
                                        Field "Id" "14d0b39f-06d3-41d4-a9ec-3242408da069"
                                    })
                            })
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with array of complex of json field")


            TestCaseData(
                """"message": 
                        "Returning next host, parameters: 
                            [
                                ( \"request\": 123-123-123 ),
                                ( \"request\": 123-123-123 )
                            ] ."
                """,
                TechField.MessageBoddiedWithPostfix (
                    "Returning next host, parameters:",
                    jsonLog {
                        Field "request" "123-123-123"
                        Field "request" "123-123-123"
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with two simple Field")

            TestCaseData(
                """"message": 
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
                TechField.MessageBoddiedWithPostfix (
                    "Incoming POST request to /abc/v1.0/deploy/1234abcd-abcd-11ff-bada-000011112222, parameters:",
                    jsonLog {
                        Field "tokenId" "11b145b64-44gh-36wg-7rja-33654rerw34"
                        Field "request" "RequestDto"
                            (jsonLog {
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
                            })
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with complex list")
        }
