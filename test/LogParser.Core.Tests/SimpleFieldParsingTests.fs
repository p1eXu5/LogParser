namespace LogParser.Core.Tests

open NUnit.Framework

[<Category("SimpleFieldParsingTests")>]
module SimpleFieldParsingTests =

    open FsUnit
    open FsToolkit.ErrorHandling

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open LogParser.Core.Tests.Factories
    open LogParser.Core.Tests.ShouldExtensions


    [<Category("p_fieldStringValue")>]
    [<TestCase(@""" bar """)>]
    [<TestCase(@"""\""bar\"" """)>]
    [<TestCase(@""" \""bar\"", \""baz\"" """)>]
    let ``1_ - p_fieldStringValue test`` (input: string) =
        result {
            let! res = runResult (p_fieldStringValue QUOTES) input
            res |> should equal (input.Trim('\"'))
        } |> Result.runTest


    [<TestCaseSource(typeof<SimpleFieldTestCases>, nameof SimpleFieldTestCases.SpecialFields)>]
    let ``special field tests`` (input: string, expected: TechField) =
        result {
            let! res = runResult (p_specialField QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<SimpleFieldTestCases>, nameof SimpleFieldTestCases.PrimitiveFields)>]
    let ``primitive field tests`` (input: string, expected: TechField) =
        result {
            let! res =
                runResult (p_primitiveField QUOTES) input
                |> Result.orElseWith (fun err ->
                    TestContext.WriteLine(err)
                    runResult (p_primitiveField NO_QUOTES) input
                )
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<SimpleFieldTestCases>, nameof SimpleFieldTestCases.ArrayFields)>]
    let ``array field tests`` (input: string, expected: TechField) =
        result {
            let! res = runResult (p_arrayPrimitiveField QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest
