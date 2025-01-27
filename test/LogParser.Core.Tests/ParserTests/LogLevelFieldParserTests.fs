namespace LogParser.Core.Tests.ParserTests

open NUnit.Framework

[<Category("04: p_logLevelField")>]
module LogLevelFieldParserTests =

    open Microsoft.Extensions.Logging

    open FsUnit
    open LogParser.Core.Tests.ShouldExtensions

    open FsToolkit.ErrorHandling

    open LogParser.Core.Types
    open LogParser.Core.Parser
    open System.Collections

    let testCases : IEnumerable =
        seq {
            [| box "\"logLevel\":\"Debug\""; QUOTES; LogLevel.Debug |]
            [| box "\"level\":\"info\""; QUOTES; LogLevel.Information |]
            [| box "\"@l\":\"VRB\""; QUOTES; LogLevel.Trace |]
            // TODO: FIX-1.0: fix no quoted
            //[| box "logLevel:Debug"; NO_QUOTES; LogLevel.Debug |]
            //[| box "level: info "; NO_QUOTES; LogLevel.Info |]
            //[| box "@l: VRB"; NO_QUOTES; LogLevel.Trace |]
        }

    [<TestCaseSource(nameof testCases)>]
    let ``p_logLevelField success with `` (input: string, quotes: string, logLevel: LogLevel) =
        result {
            let! res = runResult (p_logLevelField quotes) input
            match res with
            | TechField.Level l ->
                l |> shouldL equal logLevel "Not expected LogLevel "
                return ()
            | _ ->
                return! Error "p_logLevelField returns not TechField.Level"
        }
        |> Result.runTest

