namespace LogParser.Core.Tests.ParserTests

open NUnit.Framework

[<Category("01: p_fieldIdentifier")>]
module FieldIdentifierParserTests =

    open FsUnit
    open LogParser.Core.Tests.ShouldExtensions

    open FsToolkit.ErrorHandling

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open System.Collections

    let testCases : IEnumerable =
        seq {
            [| "\"asd\""; QUOTES; "asd" |]
            [| "asd"; NO_QUOTES; "asd" |]
            [| "cvv\\/cvr"; NO_QUOTES; "cvv\\/cvr" |]
            [| "\"cvv\\/cvr\""; QUOTES; "cvv\\/cvr" |]
            [| "\"/cvr\""; QUOTES; "/cvr" |]
        }

    [<TestCaseSource(nameof testCases)>]
    let ``p_fieldIdentifier success with `` (input: string, quotes: string, output: string) =
        result {
            let! res = runResult (p_fieldIdentifier quotes) input
            res |> should equal output
        }
        |> Result.runTest

