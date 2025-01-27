namespace LogParser.Core.Tests

open NUnit.Framework
open FsUnit

[<Category("JsonAndBodyParsingTests")>]
module JsonAndBodyParsingTests =

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open LogParser.Core.Tests.Factories
    open LogParser.Core.Tests.ShouldExtensions
    open FsToolkit.ErrorHandling


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.JsonFields)>]
    let ``10 - p_jsonField tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_jsonField' q = runResult (p_jsonField q) input

            // act
            let! res =
                p_jsonField' QUOTES
                |> Result.orElse (p_jsonField' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.ArrayJsonAnnonimous)>]
    let ``21 - p_arrayJsonAnnonimous tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_arrayJsonAnnonimous' q = runResult (p_arrayJsonAnnonimous q) input

            // act
            let! res =
                p_arrayJsonAnnonimous' QUOTES
                |> Result.orElse (p_arrayJsonAnnonimous' ESCAPED_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.ArrayJson)>]
    let ``11 - p_arrayJson tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_arrayJson' q = runResult (p_arrayJson q) input

            // act
            let! res =
                p_arrayJson' QUOTES
                |> Result.orElseWith (fun err ->
                    TestContext.WriteLine(err |> sprintf "%A")
                    p_arrayJson' ESCAPED_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.JsonAnnotated)>]
    let ``12 - p_jsonAnnotated tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_jsonAnnotated' q = runResult (p_jsonAnnotated q) input

            // act
            let! res = // p_jsonAnnotated' DOUBLE_QUOTES
                p_jsonAnnotated' ESCAPED_QUOTES
                |> Result.orElse (p_jsonAnnotated' QUOTES)
                |> Result.orElse (p_jsonAnnotated' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<Test>]
    let ``12 - p_annotation NO_QUOTES empty annotation returns error``() =
        // arrange
        let input = "\"{"

        // act
        let res = runResult (p_annotation NO_QUOTES) input

        // assert
        match res with
        | Error _ -> ()
        | Ok ok -> raise (AssertionException($"Input: {input}\n\nResult is Ok:\n%A{ok}\n\"\"\"\n%O{ok}\n\"\"\""))

    [<TestCase("\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"")>]
    [<TestCase("\"fullMessage\": \"\"\"{\"timestamp\": \"2022-07-15T04:02:47.002Z\"}\"\"\"")>]
    let ``12 - p_jsonAnnotated returns error when parses jsonField``(input: string) =
        // arrange

        // act
        let res = runResult (p_jsonAnnotated QUOTES) input

        // assert
        match res with
        | Error _ -> ()
        | Ok ok -> raise (AssertionException($"Input: {input}\n\nResult is Ok:\n%A{ok}\n\"\"\"\n%O{ok}\n\"\"\""))


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.JsonAnnotatedValue)>]
    let ``13 - p_jsonAnnotatedValue tests`` (input: string, expected: JsonAnnotated) =
        result {
            let! res = runResult (p_jsonAnnotatedValue QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.ArrayJsonAnnotated)>]
    let ``14 - p_arrayJsonAnnotated tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_arrayJsonAnnotated' q = runResult (p_arrayJsonAnnotated q) input

            // act
            let! res =
                p_arrayJsonAnnotated' QUOTES
                |> Result.orElse (p_arrayJsonAnnotated' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } 
        |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.FullMessage)>]
    let ``15 - p_jsonField full message tests`` (input: string, expected: TechField) =
        result {
            // arrange
            let p_jsonField' q = runResult (p_jsonField q) input

            // act
            let! res =
                p_jsonField' QUOTES
                |> Result.orElse (p_jsonField' NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } 
        |> Result.runTest


    [<TestCaseSource(typeof<JsonAndBodyFieldTestCases>, nameof JsonAndBodyFieldTestCases.BodyFields)>]
    let ``20 - p_body tests`` (input: string, expected: TechField) =
        result {
            let! res = runResult (p_body QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest