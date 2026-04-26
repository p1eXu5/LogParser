namespace LogParser.Core.Tests.TechLogParserTests

open System.Collections
open System.Net

open Microsoft.Extensions.Logging
open NUnit.Framework

open LogParser.Core.Types
open LogParser.Core.Dsl

type MessageCases() =

    /// 42. To split on two cases, Message, Message2 (fix Test Explorer Category splitting.)
    static member MessageFactory(n: int) : IEnumerable =
        let testCaseName = sprintf "%i - %s" n
        let testCase msg expected =
            TestCaseData(msg, TechJsonLogField.Message expected).SetName(msg |> testCaseName)
        seq {
            testCase
                "\"message\": \"Returning next host: rabbitmq_node:5672\""
                "Returning next host: rabbitmq_node:5672"

            testCase
                "Message: \\\"Bad status code 404: \\\""
                "Bad status code 404: "

            testCase
                "\"message\": \"!!!!!!!!!!!!!!!!\""
                "!!!!!!!!!!!!!!!!"

            testCase
                "\"message\": \"Request:\nMethod: GET\nPathBase: \nPath: /api/v1/internal/groups\nQueryString: ?userId=84062c29-2e0c-42f0-9d0c-5a11c7c6baf0\""
                "Request:\nMethod: GET\nPathBase: \nPath: /api/v1/internal/groups\nQueryString: ?userId=84062c29-2e0c-42f0-9d0c-5a11c7c6baf0"
        }

    static member Message : IEnumerable = MessageStringCases.MessageFactory(2)