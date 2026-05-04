namespace LogParser.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Types
open LogParser.Dsl

type MessageBodiedWithPostfixCases() =

    /// 44
    static member MessageBodiedWithPostfix : IEnumerable =
        seq {
            TestCaseData(
                """"message": 
                        "Returning next host: {
                                \"rabbitmq_node\":5672
                            } (status=Error)"
                """,
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
                    "Returning next host, parameters:", 
                    jsonLog {
                        Field "request" "123-123-123"
                    },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with simple list")

            TestCaseData(
                "\"message\": \"foo parameters: [ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] . \" ",
                TechJsonLogField.MessageBoddiedWithPostfix (
                    "foo parameters:",
                    jsonLog { Field "request" "DTO" (jsonLog { Field "rabbitmq_node" 5672}) },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with double escaped quotes list")

            TestCaseData(
                "\"message\": \"foo parameters: [ ( \"request\": DTO { rabbitmq_node:5672 } ) ] . \" ",
                TechJsonLogField.MessageBoddiedWithPostfix (
                    "foo parameters:",
                    jsonLog { Field "request" "DTO" (jsonLog { Field "rabbitmq_node" 5672}) },
                    "."
                )
            ).SetName("MessageBodiedWithPostfix with double quotes list")

            TestCaseData(
                "\"message\": \"foo parameters: DTO { rabbitmq_node:5672 }. \" ",
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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
                TechJsonLogField.MessageBoddiedWithPostfix (
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

            TestCaseData(
                """"message": "События CardStatusChangedEvent успешно обработано: { \"cardId\": \"759062f9-c614-4c32-933a-995cd2ad07cf\", \"cardHmac\": \"aafe****\", \"status\": \"Inactive\", \"reason\": \"ByBank\", \"alias\": \"fromASBank\", \"privetPan\": \"aafecb7959b4ab2c8da09c6b526843a93e56d731508b5345c08eed547d7f73f1\" }. Идентификатор сообщения 14320000-9a3c-0005-2fd5-08de36408863, пользователь: 760acf97-daa4-4508-9c08-12eab130f8bb."
                """,
                TechJsonLogField.MessageBoddiedWithPostfix (
                    "События CardStatusChangedEvent успешно обработано:",
                    jsonLog {
                        Field "cardId" "759062f9-c614-4c32-933a-995cd2ad07cf"
                        Field "cardHmac" "aafe****"
                        Field "status" "Inactive"
                        Field "reason" "ByBank"
                        Field "alias" "fromASBank"
                        Field "privetPan" "aafecb7959b4ab2c8da09c6b526843a93e56d731508b5345c08eed547d7f73f1"
                    },
                    ". Идентификатор сообщения 14320000-9a3c-0005-2fd5-08de36408863, пользователь: 760acf97-daa4-4508-9c08-12eab130f8bb."
                )
            ).SetName("MessageBodiedWithPostfix with complex list and postfix")
        }