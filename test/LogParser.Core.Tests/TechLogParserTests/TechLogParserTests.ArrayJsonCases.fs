namespace LogParser.Core.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type ArrayJsonCases() =

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
                TechJsonLogField.ArrayJson (
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
                TechJsonLogField.ArrayJson (
                    "certificates",
                    [
                        jsonLog { Field "usage" "CA" }
                    ]
                )
            ).SetName("11 - ArrayJson. with escaped quotes single line")

            TestCaseData(
                """\"certificates\":\"[ { \"usage\":\"CA\" } ]\"
                """,
                TechJsonLogField.ArrayJson (
                    "certificates",
                    [
                        jsonLog { Field "usage" "CA" }
                    ]
                )
            ).SetName("11 - ArrayJson. with escaped quotes single line wrapped with quotes")


            TestCaseData(
                """\"certificates\":[ 
                        { \"usage\":\"CA\" } 
                    ]
                """,
                TechJsonLogField.ArrayJson (
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
                TechJsonLogField.ArrayJson (
                    "foo",
                    [
                        [TechJsonLogField.ArrayJsonAnnonimous [
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
                TechJsonLogField.ArrayJson (
                    "vals",
                    [
                        [TechJsonLogField.StringAnnonimous "some message";]
                        [TechJsonLogField.IntAnnonimous 0;]
                        [TechJsonLogField.StringAnnonimous "0ca140ed-2608-42a5-a0da-6c2665f88a65";]
                        
                        jsonLog {
                            Field "HTTPHeader_content-type" "application/json"
                            Field "HTTPHeader_x-request-id" "0ca140ed-2608-42a5-a0da-6c2665f88a65"
                            Field "HTTPPath" "/debitint"
                        }
                        
                        [TechJsonLogField.NullAnnonimous]
                    ]
                )
            ).SetName("11 - ArrayJson. mix of nested json, string, int and null")
        }