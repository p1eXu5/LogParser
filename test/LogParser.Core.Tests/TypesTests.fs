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

        TechnoField.toString json.Fields
        |> should equal expected


