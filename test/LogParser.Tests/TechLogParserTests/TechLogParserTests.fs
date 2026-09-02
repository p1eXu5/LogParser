namespace LogParser.Tests.TechLogParserTests

open System
open NUnit.Framework

module TechLogParserTests =

    open FsUnit
    open LogParser.Tests.ShouldExtensions

    open Microsoft.Extensions.Logging
    open FsToolkit.ErrorHandling
    open p1eXu5.FSharp.Reactive.Testing

    open LogParser
    open LogParser.Dsl
    open LogParser.Types
    open System.Collections

    let private testScheduler = TestScheduler.create ()
    let private testObserver = TestScheduler.createObserver<TechLogPosition> testScheduler

    // ------------------------------------
    // p_fieldIdentifier tests
    // ------------------------------------

    let ``p_fieldIdentifier cases`` : IEnumerable =
        seq {
            [| "\"asd\""; TechLogParser.QUOTES; "asd" |]
            [| "asd"; TechLogParser.NO_QUOTES; "asd" |]
            [| "cvv\\/cvr"; TechLogParser.NO_QUOTES; "cvv\\/cvr" |]
            [| "\"cvv\\/cvr\""; TechLogParser.QUOTES; "cvv\\/cvr" |]
            [| "\"/cvr\""; TechLogParser.QUOTES; "/cvr" |]
        }

    [<TestCaseSource(nameof ``p_fieldIdentifier cases``)>]
    let ``p_fieldIdentifier success with `` (input: string, quotes: string, output: string) =
        result {
            let! res = runResult (TechLogParser.p_fieldIdentifier quotes) input
            res |> should equal output
        }
        |> Result.runTest

    // ------------------------------------
    // p_fieldStringValue tests
    // ------------------------------------

    [<TestCase(@""" bar """)>]
    [<TestCase(@"""\""bar\"" """)>]
    [<TestCase(@""" \""bar\"", \""baz\"" """)>]
    let ``p_fieldStringValue test`` (input: string) =
        result {
            let! res = runResult (TechLogParser.p_fieldStringValue TechLogParser.QUOTES) input
            res |> should equal (input.Trim('\"'))
        } |> Result.runTest

    // ------------------------------------
    // p_stringField tests
    // ------------------------------------

    let ``p_stringField cases`` : IEnumerable =
        seq {
            [| "\"asd\":\"asd\""; TechLogParser.QUOTES; "asd"; "asd" |]
            // TODO: FIX-1.0: fix no quoted
            // [| "asd: asd"; NO_QUOTES; "asd"; "asd" |]
            // [| "cvv\\/cvr: cvv\\/cvr"; NO_QUOTES; "cvv\\/cvr"; "cvv\\/cvr" |]
            [| "\"cvv\\/cvr\": \"cvv\\/cvr\""; TechLogParser.QUOTES; "cvv\\/cvr"; "cvv\\/cvr" |]
            [| "\"/cvr\":\"/cvr\""; TechLogParser.QUOTES; "/cvr"; "/cvr" |]
        }

    [<TestCaseSource(nameof ``p_stringField cases``)>]
    let ``p_stringField success with `` (input: string, quotes: string, key: string, value: string) =
        result {
            let! res = runResult (TechLogParser.p_stringField quotes) input
            match res with
            | TechJsonLogField.String (k, v) ->
                k |> should equal key
                v |> should equal value
                return ()
            | _ ->
                return! Error "p_stringField returns not TechField.String"
        }
        |> Result.runTest

    // ------------------------------------
    // p_logLevelField tests
    // ------------------------------------

    let ``p_logLevelField cases`` : IEnumerable =
        seq {
            [| box "\"logLevel\":\"Debug\""; TechLogParser.QUOTES; LogLevel.Debug |]
            [| box "\"level\":\"info\""; TechLogParser.QUOTES; LogLevel.Information |]
            [| box "\"@l\":\"VRB\""; TechLogParser.QUOTES; LogLevel.Trace |]
            // TODO: FIX-1.0: fix no quoted
            //[| box "logLevel:Debug"; NO_QUOTES; LogLevel.Debug |]
            //[| box "level: info "; NO_QUOTES; LogLevel.Info |]
            //[| box "@l: VRB"; NO_QUOTES; LogLevel.Trace |]
        }

    [<TestCaseSource(nameof ``p_logLevelField cases``)>]
    let ``p_logLevelField success with `` (input: string, quotes: string, logLevel: LogLevel) =
        result {
            let! res = runResult (TechLogParser.p_logLevelField quotes) input
            match res with
            | TechJsonLogField.Level l ->
                l |> shouldL equal logLevel "Not expected LogLevel "
                return ()
            | _ ->
                return! Error "p_logLevelField returns not TechField.Level"
        }
        |> Result.runTest

    // ------------------------------------
    // p_primitiveField tests
    // ------------------------------------

    [<TestCaseSource(typeof<PrimitiveFieldCases>, nameof PrimitiveFieldCases.PrimitiveFields)>]
    let ``p_primitiveField tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res =
                runResult (TechLogParser.p_primitiveField TechLogParser.QUOTES) input
                |> Result.orElseWith (fun err ->
                    TestContext.WriteLine(err)
                    runResult (TechLogParser.p_primitiveField TechLogParser.NO_QUOTES) input
                )
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // p_arrayPrimitiveField tests
    // ------------------------------------

    [<TestCaseSource(typeof<ArrayPrimitiveFieldCases>, nameof ArrayPrimitiveFieldCases.ArrayFields)>]
    let ``p_arrayPrimitiveField tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res = runResult (TechLogParser.p_arrayPrimitiveField TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // p_specialField tests
    // ------------------------------------

    [<TestCaseSource(typeof<SpecialFieldCases>, nameof SpecialFieldCases.SpecialFields)>]
    let ``p_specialField tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res = runResult (TechLogParser.p_specialField TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // p_jsonField tests
    // ------------------------------------

    [<TestCaseSource(typeof<JsonFieldCases>, nameof JsonFieldCases.JsonFields)>]
    let ``p_jsonField tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_jsonField' q = runResult (TechLogParser.p_jsonField q) input

            // act
            let! res =
                p_jsonField' TechLogParser.QUOTES
                |> Result.orElse (p_jsonField' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest


    [<TestCaseSource(typeof<JsonFieldCases>, nameof JsonFieldCases.FullMessage)>]
    let ``p_jsonField fullMessage field tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_jsonField' q = runResult (TechLogParser.p_jsonField q) input

            // act
            let! res =
                p_jsonField' TechLogParser.QUOTES
                |> Result.orElse (p_jsonField' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } 
        |> Result.runTest

    // ------------------------------------
    // p_arrayJsonAnnonimous tests
    // ------------------------------------

    [<TestCaseSource(typeof<ArrayJsonAnnonimousCases>, nameof ArrayJsonAnnonimousCases.ArrayJsonAnnonimous)>]
    let ``p_arrayJsonAnnonimous tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_arrayJsonAnnonimous' q = runResult (TechLogParser.p_arrayJsonAnnonimous q) input

            // act
            let! res =
                p_arrayJsonAnnonimous' TechLogParser.QUOTES
                |> Result.orElse (p_arrayJsonAnnonimous' TechLogParser.ESCAPED_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_arrayJson tests
    // ------------------------------------

    [<TestCaseSource(typeof<ArrayJsonCases>, nameof ArrayJsonCases.ArrayJson)>]
    let ``p_arrayJson tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_arrayJson' q = runResult (TechLogParser.p_arrayJson q) input

            // act
            let! res =
                p_arrayJson' TechLogParser.QUOTES
                |> Result.orElseWith (fun err ->
                    TestContext.WriteLine(err |> sprintf "%A")
                    p_arrayJson' TechLogParser.ESCAPED_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_arrayJsonAnnotated tests
    // ------------------------------------

    [<TestCaseSource(typeof<ArrayJsonAnnotatedCases>, nameof ArrayJsonAnnotatedCases.ArrayJsonAnnotated)>]
    let ``p_arrayJsonAnnotated tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_arrayJsonAnnotated' q = runResult (TechLogParser.p_arrayJsonAnnotated q) input

            // act
            let! res =
                p_arrayJsonAnnotated' TechLogParser.QUOTES
                |> Result.orElse (p_arrayJsonAnnotated' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } 
        |> Result.runTest

    // ------------------------------------
    // p_annotation tests
    // ------------------------------------

    [<Test>]
    let ``p_annotation NO_QUOTES empty annotation returns error``() =
        // arrange
        let input = "\"{"

        // act
        let res = runResult (TechLogParser.p_annotation TechLogParser.NO_QUOTES) input

        // assert
        match res with
        | Error _ -> ()
        | Ok ok -> raise (AssertionException($"Input: {input}\n\nResult is Ok:\n%A{ok}\n\"\"\"\n%O{ok}\n\"\"\""))

    // ------------------------------------
    // p_jsonAnnotatedValue tests
    // ------------------------------------

    [<TestCaseSource(typeof<JsonAnnotatedValueCases>, nameof JsonAnnotatedValueCases.JsonAnnotatedValue)>]
    let ``p_jsonAnnotatedValue tests`` (input: string, expected: JsonAnnotated) =
        result {
            let! res = runResult (TechLogParser.p_jsonAnnotatedValue TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    // ------------------------------------
    // p_jsonAnnotated tests
    // ------------------------------------

    [<TestCase("\"requestBody\": \"{ \\\"bankId\\\": \\\"1234\\\"}\"")>]
    [<TestCase("\"fullMessage\": \"\"\"{\"timestamp\": \"2022-07-15T04:02:47.002Z\"}\"\"\"")>]
    let ``p_jsonAnnotated returns error when parses jsonField``(input: string) =
        // arrange

        // act
        let res = runResult (TechLogParser.p_jsonAnnotated TechLogParser.QUOTES) input

        // assert
        match res with
        | Error _ -> ()
        | Ok ok -> raise (AssertionException($"Input: {input}\n\nResult is Ok:\n%A{ok}\n\"\"\"\n%O{ok}\n\"\"\""))


    [<TestCaseSource(typeof<JsonAnnotatedCases>, nameof JsonAnnotatedCases.JsonAnnotated)>]
    let ``p_jsonAnnotated tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_jsonAnnotated' q = runResult (TechLogParser.p_jsonAnnotated q) input

            // act
            let! res = // p_jsonAnnotated' DOUBLE_QUOTES
                p_jsonAnnotated' TechLogParser.ESCAPED_QUOTES
                |> Result.orElse (p_jsonAnnotated' TechLogParser.QUOTES)
                |> Result.orElse (p_jsonAnnotated' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_body tests
    // ------------------------------------

    [<TestCaseSource(typeof<BodyCases>, nameof BodyCases.BodyFields)>]
    let ``p_body tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res = runResult (TechLogParser.p_body TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // p_jsonSpecialPrimitiveInBraces tests
    // ------------------------------------

    [<Test>]
    [<Category("TechField Message parsing: p_messageJsonAnnotated")>]
    let ``p_jsonSpecialPrimitiveInBraces test`` () =
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
                } |> TechJsonLogField.JsonAnnotated
            let! res = runResult (TechLogParser.p_jsonSpecialPrimitiveInBraces TechLogParser.ESCAPED_QUOTES) input
            res |> should equal expected
        } |> Result.runTest

    // ------------------------------------
    // p_messageJsonList tests
    // ------------------------------------

    [<Category("TechField Message parsing: p_messageJsonAnnotatedList")>]
    [<TestCaseSource(typeof<MessageJsonListCases>, nameof MessageJsonListCases.MessageJsonList)>]
    let ``p_messageJsonList test`` (input: string, expected: TechJsonLogField list) =
        result {
            // arrange
            let p_messageJsonAnnotatedList' q = runResult (TechLogParser.p_messageJsonList q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_messageJsonAnnotatedList' TechLogParser.ESCAPED_QUOTES
                |> Result.orElse (p_messageJsonAnnotatedList' TechLogParser.QUOTES)
                |> Result.orElse (p_messageJsonAnnotatedList' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equivalent expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_messageString tests
    // ------------------------------------

    [<TestCaseSource(typeof<MessageStringCases>, nameof MessageStringCases.MessageString)>]
    [<Category("TechField Message parsing: p_messageString")>]
    let ``p_messageString tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_messageString' q = runResult (TechLogParser.p_messageString q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_messageString' TechLogParser.QUOTES
                |> Result.orElse (p_messageString' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_message tests
    // ------------------------------------

    [<TestCaseSource(typeof<MessageCases>, nameof MessageCases.Message)>]
    [<Category("TechField Message parsing: p_message on simple message")>]
    let ``p_message tests`` (input: string, expected: TechJsonLogField) =
        result {
            // arrange
            let p_message' q = runResult (TechLogParser.p_message q) input

            // act
            let! res = // p_messageString' NO_QUOTES
                p_message' TechLogParser.QUOTES
                |> Result.orElse (p_message' TechLogParser.NO_QUOTES)

            // assert
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        }
        |> Result.runTest

    // ------------------------------------
    // p_messageBuddied tests
    // ------------------------------------

    [<Category("TechField Message parsing: p_messageBuddied")>]
    [<TestCaseSource(typeof<MessageBodiedCases>, nameof MessageBodiedCases.MessageBodied)>]
    let ``p_messageBuddied tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res = runResult (TechLogParser.p_messageBuddied TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // p_messageBuddiedWithPostfix tests
    // ------------------------------------

    [<TestCaseSource(typeof<MessageBodiedWithPostfixCases>, nameof MessageBodiedWithPostfixCases.MessageBodiedWithPostfix)>]
    [<Category("TechField Message parsing: p_messageBuddiedWithPostfix")>]
    let ``p_messageBuddiedWithPostfix tests`` (input: string, expected: TechJsonLogField) =
        result {
            let! res = runResult (TechLogParser.p_messageBuddiedWithPostfix TechLogParser.QUOTES) input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest

    // ------------------------------------
    // parse tests
    // ------------------------------------

    [<Test>]
    let ``parse - docker text log test`` () =
        result {
            let input = 
                """
                    foo
                    bar
                    DEBUG ONLY!!! Sent packet length: 475
                """

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 3
            res
            |> List.map (function
                | TextLog _ -> true
                | JsonLog _ -> false
            )
            |> should not' (contain false)
        } |> Result.runTest

    [<Test>]
    let ``parse - empty json`` () =
        result {
            let input = 
                """{}"""

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 1
            res[0] |> should be (equal (TechLog.JsonLog ({Source = None; Fields = []})))
        } |> Result.runTest

    [<Test>]
    let ``parseStream - empty json`` () =
        result {
            let input =
                """{}"""

            let observed = ResizeArray<TechLogPosition>()
            let mutable completed = false

            let observer =
                { new IObserver<TechLogPosition> with
                    member _.OnNext value = observed.Add value
                    member _.OnError ex = raise ex
                    member _.OnCompleted() = completed <- true }

            use stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes input)

            // Act:
            do! LogParser.TechLogParser.parseStream observer stream

            // Assert:
            completed |> should be True
            observed |> should haveCount 1
            observed[0].Log |> should be (equal (TechLog.JsonLog ({Source = None; Fields = []})))
        } |> Result.runTest

    [<Test>]
    let ``parseStream - file with empty json`` () =
        let path = IO.Path.GetTempFileName()

        try
            IO.File.WriteAllText(path, """{}""", Text.Encoding.UTF8)

            result {
                let observed = ResizeArray<TechLogPosition>()
                let mutable completed = false

                let observer =
                    { new IObserver<TechLogPosition> with
                        member _.OnNext value = observed.Add value
                        member _.OnError ex = raise ex
                        member _.OnCompleted() = completed <- true }

                use stream = new IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read)

                // Act:
                do! LogParser.TechLogParser.parseStream observer stream

                // Assert:
                completed |> should be True
                observed |> should haveCount 1
                observed[0].Log |> should be (equal (TechLog.JsonLog ({Source = None; Fields = []})))
            } |> Result.runTest
        finally
            IO.File.Delete path

    [<Test>]
    let ``parse - log source test`` () =
        result {
            let input = "test source { \"message\": \"foo\" }"

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 1
            match res |> List.head with
            | JsonLog tl ->
                tl.Source |> should equal (TechJsonLogField.String ("logSource", "test source") |> Some)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``parse - empty log source test`` () =
        result {
            let input = "     { \"message\": \"foo\" }"

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 1
            match res |> List.head with
            | JsonLog tl ->
                tl.Source |> should be (ofCase <@ None @>)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``parse - logContext with message test`` () =
        result {
            let input = "{\"logContext\":\"{\\\"Message\\\": \\\"asd\\\"}\"}"
            let expected =
                jsonLog {
                    Field "logContext" (jsonLog { message "asd" })
                }
                |> List.head

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 1
            match res |> List.head with
            | JsonLog tl ->
                tl.Fields |> should haveLength 1
                tl.Fields.[0] |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" (tl.Fields.[0]) expected)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"
        } |> Result.runTest


    [<Test>]
    let ``parse - logContext with message with wrapped array test`` () =
        result {
            let input = "{\"logContext\":\"{\\\"Message\\\": \\\"[{\\\"code\\\": \\\"asd\\\"}]\\\"}\"}"
            let expected =
                jsonLog {
                    Field "logContext" (jsonLog { message ([jsonLog { Field "code" "asd" }]) })
                }
                |> List.head

            // Act:
            let! res = LogParser.TechLogParser.parse testObserver input

            // Assert:
            res |> should haveLength 1
            match res |> List.head with
            | JsonLog tl ->
                tl.Fields |> should haveLength 1
                tl.Fields.[0] |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" (tl.Fields.[0]) expected)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"
        } |> Result.runTest


    [<TestCaseSource(typeof<ParseCases>, nameof ParseCases.Parse)>]
    let ``tech log parsing tests`` (input: string, expected: TechLog) =
        result {
            let! res = LogParser.TechLogParser.parse testObserver input
            res |> List.head |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" (res |> List.head) expected)
        } |> Result.runTest