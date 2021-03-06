namespace LogParser.Core.Tests

open LogParser.Core


module ParserTests =

    open NUnit.Framework
    open FsUnit
    open LogParser.Core.Types
    open LogParser.Core.Parser
    open LogParser.Core.Tests.Factories
    open LogParser.Core.Tests.ShouldExtensions
    open FParsec
    open FsToolkit.ErrorHandling
    open LogParser.Core.Dsl


    [<TestCase(@""" bar """)>]
    [<TestCase(@"""\""bar\"" """)>]
    [<TestCase(@""" \""bar\"", \""baz\"" """)>]
    let ``field parsing. string value test`` (input: string) =
        result {
            let! res = runResult (fieldStringValue "\"") input
            res |> should equal (input.Trim('\"'))
        } |> Result.runTest


    [<Test>]
    let ``field parsing. typeJson test`` () =
        result {
            let input = "\\\"request\\\": DTO {rabbitmq_node:5672}"
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        } |> Log.fieldList
                }
            let! res = runResult (typeJson (fieldIdentifier "\\\"")) input
            res |> should equal expected
        } |> Result.runTest


    [<Test>]
    let ``field parsing. typeJson with string field without quotes test`` () =
        result {
            let input = "\\\"request\\\": DTO {rabbitmq_node: ABC}"
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" "ABC"
                        } |> Log.fieldList
                }
            let! res = runResult (typeJson (fieldIdentifier "\\\"")) input
            res |> should equal expected
        } |> Result.runTest


    [<Test>]
    let ``field parsing. message parameter test`` () =
        result {
            let input = "(\\\"request\\\": DTO {rabbitmq_node:5672})"
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        } |> Log.fieldList
                } |> MessageParameter.TypeJson
            let! res = runResult messageParameter input
            res |> should equal expected
        } |> Result.runTest


    [<Test>]
    let ``field parsing. message parameter list test`` () =
        result {
            let input = "[ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] "
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        } |> Log.fieldList
                } |> MessageParameter.TypeJson
            let! res = runResult messageParameterList input
            res |> should haveLength 1
            res |> List.head |> should equal expected
        } |> Result.runTest


    [<Test>]
    let ``field parsing. messageParameterized test`` () =
        result {
            let input = "\"message\": \"foo parameters: [ ( \\\"request\\\": DTO { rabbitmq_node:5672 } ) ] . \" "
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        } |> Log.fieldList
                } |> MessageParameter.TypeJson
            let! res = runResult messageParameterized input
            match res with
            | TechnoField.MessageParameterized (_, e) ->
                e |> should haveLength 1
                e |> List.head |> should equal expected
                return! Result.Ok ()
            | _ ->
                return! Result.Error "wrong log type. Log type is TextLog" 
        } |> Result.runTest


    [<Test>]
    let ``field parsing. messageParameterized without brakets test`` () =
        result {
            let input = "\"message\": \"foo parameters: DTO { rabbitmq_node:5672 }. \" "
            let expected =
                {
                    Key = "parameters"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                        } |> Log.fieldList
                } |> MessageParameter.TypeJson

            let! res = runResult messageParameterized input

            match res with
            | TechnoField.MessageParameterized (_, e) ->
                e |> should haveLength 1
                e |> List.head |> should equal expected
                return! Result.Ok ()
            | _ ->
                return! Result.Error "wrong log type. Log type is TextLog" 
        } |> Result.runTest


    [<Test>]
    let ``field parsing. messageParameterized with nested parameter test`` () =
        result {
            let input = """
                "message": "foo parameters: [( 
                    \"request\": DTO { 
                        rabbitmq_node:5672,
                        DeviceMetadata: DeviceMetadata { 
                            Scoring: DeviceMetadataScoring { 
                              DeviceCountry: \"BR\"
                            }
                        }
                    })] . " 
            """
            let expected =
                {
                    Key = "request"
                    TypeName = "DTO"
                    Body =
                        jsonLog {
                            Field "rabbitmq_node" 5672
                            Field "DeviceMetadata" "DeviceMetadata"
                                (jsonLog {
                                    Field "Scoring" "DeviceMetadataScoring"
                                        (jsonLog {
                                            Field "DeviceCountry" "BR"
                                        })
                                })
                        } |> Log.fieldList
                } |> MessageParameter.TypeJson
            let! res = runResult messageParameterized input
            match res with
            | TechnoField.MessageParameterized (_, e) ->
                e |> should haveLength 1
                e |> List.head |> should equal expected
                return! Result.Ok ()
            | _ ->
                return! Result.Error "wrong log type. Log type is TextLog" 
        } |> Result.runTest


    [<TestCaseSource(typeof<ParserTestCases>, nameof ParserTestCases.FieldTests)>]
    let ``field parsing tests`` (input: string, expected: TechnoField) =
        result {
            let! res = runResult field input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<ParserTestCases>, nameof ParserTestCases.InnerField)>]
    let ``inner field parsing tests`` (input: string, expected: TechnoField) =
        result {
            let! res = runResult innerField input
            res |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" res expected)
        } |> Result.runTest


    [<TestCaseSource(typeof<ParserTestCases>, nameof ParserTestCases.LogTests)>]
    let ``log parsing tests`` (input: string, expected: Log) =
        result {
            let! res = LogParser.Core.Parser.parse input
            res |> List.head |> shouldL equal expected (sprintf "Actual: %A\nExpected: %A" (res |> List.head) expected)
        } |> Result.runTest


    [<Test>]
    let ``docker log test`` () =
        result {
            let input = 
                """
                    foo
                    bar
                """

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 2
        } |> Result.runTest


    [<Test>]
    let ``log source test`` () =
        result {
            let input = "test source { \"message\": \"foo\" }"

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 1
            match res |> List.head with
            | TechnoLog tl ->
                tl.Source |> should equal (Some "test source")
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``empty log source test`` () =
        result {
            let input = "     { \"message\": \"foo\" }"

            let! res = LogParser.Core.Parser.parse input

            res |> should haveLength 1
            match res |> List.head with
            | TechnoLog tl ->
                tl.Source |> should be (ofCase <@ None @>)
                return! Result.Ok ()
            | TextLog _ ->
                return! Result.Error "wrong log type. Log type is TextLog"

        } |> Result.runTest


    [<Test>]
    let ``kibana fullMessage test`` () =
        let log = """{"traceId":"9fdl11e047ace655822c5417d02843f7","timestamp":"2012-07-23T09:03:06.036Z"}"""
        let fullMessage = $"\"\"\"{log}\"\"\""

        FParsec.runResult Kibana.fullMessage fullMessage
        |> Result.shouldEqual log


    [<Test>]
    let ``kibana log test`` () =
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
    let ``kibana2 log test`` () =
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