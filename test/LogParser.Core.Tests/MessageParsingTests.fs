namespace LogParser.Core.Tests

open NUnit.Framework
open FsUnit

module MessageParsingTests =

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open LogParser.Core.Tests.Factories
    open LogParser.Core.Tests.ShouldExtensions
    open FsToolkit.ErrorHandling
    open LogParser.Core.Dsl

    [<Test>]
    [<Category("TechField Message parsing: p_messageJsonAnnotated")>]
    let ``p_messageJsonAnnotated test`` () =
        result {
            let input = "(\\\"request\\\": DTO {rabbitmq_node:5672})"
            let expected =
                {
                    Key = "request"
                    Annotation = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        }
                } |> TechField.JsonAnnotated
            let! res = runResult (p_messageJson ESCAPED_QUOTES) input
            res |> should equal expected
        } |> Result.runTest


    [<TestCaseSource(typeof<MessageTestCases>, nameof MessageTestCases.MessageJsonAnnotatedList)>]
    [<Category("TechField Message parsing: p_messageJsonAnnotatedList")>]
    let ``p_messageJsonAnnotatedList test`` (input: string, expected: TechField list) =
        result {
            // arrange
            let p_messageJsonAnnotatedList' q = runResult (p_messageJsonList q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_messageJsonAnnotatedList' ESCAPED_QUOTES
                |> Result.orElse (p_messageJsonAnnotatedList' QUOTES)
                |> Result.orElse (p_messageJsonAnnotatedList' NO_QUOTES)

            // assert
            res |> shouldL equivalent expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<TestCaseSource(typeof<MessageTestCases>, nameof MessageTestCases.Message)>]
    [<Category("TechField Message parsing: p_messageString")>]
    let ``p_messageString tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_messageString' q = runResult (p_messageString q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_messageString' QUOTES
                |> Result.orElse (p_messageString' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    [<TestCaseSource(typeof<MessageTestCases>, nameof MessageTestCases.Message2)>]
    [<Category("TechField Message parsing: p_message on simple message")>]
    let ``p_message tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_message' q = runResult (p_message q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_message' QUOTES
                |> Result.orElse (p_message' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    [<TestCaseSource(typeof<MessageTestCases>, nameof MessageTestCases.MessageBodied)>]
    [<Category("TechField Message parsing: p_messageBuddied")>]
    let ``p_messageBuddied tests`` (input: string, expected: TechField) =
        result {
            let! res = runResult (p_messageBuddied QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<MessageTestCases>, nameof MessageTestCases.MessageBodiedWithPostfix)>]
    [<Category("TechField Message parsing: p_messageBuddiedWithPostfix")>]
    let ``p_messageBuddiedWithPostfix tests`` (input: string, expected: TechField) =
        result {
            let! res = runResult (p_messageBuddiedWithPostfix QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest
