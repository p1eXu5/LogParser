namespace LogParser.Core.Tests.ParserTests

open NUnit.Framework

[<Category("03: p_stringField")>]
module StringFieldParserTests =

    open FsUnit
    open LogParser.Core.Tests.ShouldExtensions

    open FsToolkit.ErrorHandling

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open System.Collections

    let testCases : IEnumerable =
        seq {
            [| "\"asd\":\"asd\""; QUOTES; "asd"; "asd" |]
            // TODO: FIX-1.0: fix no quoted
            // [| "asd: asd"; NO_QUOTES; "asd"; "asd" |]
            // [| "cvv\\/cvr: cvv\\/cvr"; NO_QUOTES; "cvv\\/cvr"; "cvv\\/cvr" |]
            [| "\"cvv\\/cvr\": \"cvv\\/cvr\""; QUOTES; "cvv\\/cvr"; "cvv\\/cvr" |]
            [| "\"/cvr\":\"/cvr\""; QUOTES; "/cvr"; "/cvr" |]
        }

    [<TestCaseSource(nameof testCases)>]
    let ``p_stringField success with `` (input: string, quotes: string, key: string, value: string) =
        result {
            let! res = runResult (p_stringField quotes) input
            match res with
            | TechField.String (k, v) ->
                k |> should equal key
                v |> should equal value
                return ()
            | _ ->
                return! Error "p_stringField returns not TechField.String"
        }
        |> Result.runTest

