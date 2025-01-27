namespace LogParser.Core.Tests

open LogParser.Core
open NUnit.Framework
open FsUnit

[<Category("LogParsingTests")>]
module LogParsingTests =

    open LogParser.Core.Types
    open LogParser.Core.Tests.Factories
    open LogParser.Core.Tests.ShouldExtensions
    open FsToolkit.ErrorHandling

    [<Test>]
    let ``5_ - docker text log test`` () =
        result {
            let input = 
                """
                    foo
                    bar
                    DEBUG ONLY!!! Sent packet length: 475
                """

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 3
            res
            |> List.map (function
                | TextLog _ -> true
                | TechLog _ -> false
            )
            |> should not' (contain false)
        } |> Result.runTest


    [<Test>]
    let ``5_ - log source test`` () =
        result {
            let input = "test source { \"message\": \"foo\" }"

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 1
            match res |> List.head with
            | TechLog tl ->
                tl.Source |> should equal (TechField.String ("logSource", "test source") |> Some)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``5_ - empty log source test`` () =
        result {
            let input = "     { \"message\": \"foo\" }"

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 1
            match res |> List.head with
            | TechLog tl ->
                tl.Source |> should be (ofCase <@ None @>)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``5_ - kibana fullMessage test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        let fullMessage = $"\"\"\"{log}\"\"\""

        FParsec.runResult Kibana.fullMessage fullMessage
        |> Result.shouldEqual log


    [<Test>]
    let ``5_ - kibana log test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        
        let kibana =

            let fm = $"\"\"\"{log}\"\"\""
            $"""
            {{
              "took" : 1073,
              "timed_out" : false,
              "_shards" : {{
                "total" : 13,
                "successful" : 13,
                "skipped" : 0,
                "failed" : 0
              }},
              "hits" : {{
                "total" : {{
                  "value" : 42,
                  "relation" : "eq"
                }},
                "max_score" : 0.0,
                "hits" : [
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "5ifNj4EB-fw2kdSFBGZD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }}
                  }},
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "DifNj4EB-fw2kdSFBGdD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }}
                  }}
                ]
              }}
            }}
            """

        Kibana.parse kibana
        |> Result.should equivalent [log; log]


    [<Test>]
    let ``5_ - kibana2 log test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        let fm = $"\"\"\"{log}\"\"\""

        let expected = 
            $"""{{"fullMessage" : {fm}, "TraceId" : "9fde11e047ace615822c5417d02843f7"}}""".Replace(" ", "").Replace("\r", "").Replace("\n", "")
        
        let kibana =

            let fm = $"\"\"\"{log}\"\"\""
            $"""
            {{
              "took" : 1073,
              "timed_out" : false,
              "_shards" : {{
                "total" : 13,
                "successful" : 13,
                "skipped" : 0,
                "failed" : 0
              }},
              "hits" : {{
                "total" : {{
                  "value" : 42,
                  "relation" : "eq"
                }},
                "max_score" : 0.0,
                "hits" : [
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "5ifNj4EB-fw2kdSFBGZD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }},
                    "sort" : [
                      1657601105052
                    ]
                  }},
                  {{
                    "_index" : "filebeat-2022.06.23",
                    "_type" : "_doc",
                    "_id" : "DifNj4EB-fw2kdSFBGdD",
                    "_score" : 0.0,
                    "_source" : {{
                      "fullMessage" : {fm},
                      "TraceId" : "9fde11e047ace615822c5417d02843f7"
                    }},
                    "sort" : [
                      1657601105052
                    ]
                  }}
                ]
              }}
            }}
            """

        Kibana.parse2 kibana
        |> Result.map (List.map (fun s -> s.Replace(" ", "").Replace("\r", "").Replace("\n", "")))
        |> Result.should equivalent [expected; expected]

    [<TestCaseSource(typeof<TechLogTestCases>, nameof TechLogTestCases.TechLogs)>]
    let ``tech log parsing tests`` (input: string, expected: Log) =
        result {
            let! res = LogParser.Core.Parser.parse input
            res |> List.head |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" (res |> List.head) expected)
        } |> Result.runTest