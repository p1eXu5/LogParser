namespace LogParser.Core.Tests.ParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type ArrayJsonAnnonimousCases() =

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
                TechJsonLogField.ArrayJsonAnnonimous [
                    jsonLog {
                        Field "rabbitmq_node" 
                            (jsonLog { Field "bar" "5672" })
                    }
                ]
            ).SetName("21 - ArrayJsonAnnonimous. single json")
        }