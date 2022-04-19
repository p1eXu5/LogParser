namespace LogParser.Core.Tests

open LogParser.Core.Types

module TypesTests =

    open NUnit.Framework
    open FsUnit
    open LogParser.Core.Dsl

    [<Test>]
    let ``json to string test`` () =
        let json =
            jsonLog {
                Field "foo" "foo"
                Field "bar" "bar"
            }

        let expected =
            "{\n" +
            "    \"foo\": \"foo\",\n" +
            "    \"bar\": \"bar\"\n" +
            "}"

        TechnoFields.toString 1 json.Fields
        |> should equal expected


