namespace LogParser.Tests

open LogParser.Types
open NUnit.Framework
open LogParser.Tests.ShouldExtensions

module TechJsonLogFieldTests =

    open FsUnit
    open LogParser.Dsl

    [<Test>]
    let ``toString - json to string test`` () =
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

        TechJsonLogField.toString 1 json
        |> should equal expected


    [<Test>]
    let ``toString - json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJson (
                "certificates",
                [
                    jsonLog { Field "usage" "CA" }
                ]
            )

        let expected =
            "{\n" +
            "    \"certificates\": [\n" +
            "        {\n" +
            "            \"usage\": \"CA\"\n" +
            "        }\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected

    [<Test>]
    let ``toString - multi json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJson (
                "certificates",
                [
                    jsonLog { Field "usage" "CA" }
                    jsonLog { Field "usage" "CA" }
                ]
            )

        let expected =
            "{\n" +
            "    \"certificates\": [\n" +
            "        {\n" +
            "            \"usage\": \"CA\"\n" +
            "        },\n" +
            "        {\n" +
            "            \"usage\": \"CA\"\n" +
            "        }\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``toString - annonimous json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJsonAnnonimous [
                jsonLog {
                    Field "rabbitmq_node" 
                        (jsonLog { Field "bar" "5672" })
                }
            ]

        let expected =
            "{\n" +
            "    [\n" +
            "        {\n" +
            "            \"rabbitmq_node\": {\n" +
            "                \"bar\": \"5672\"\n" +
            "            }\n" +
            "        }\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``toString - multi annonimous json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJsonAnnonimous [
                jsonLog {
                    Field "rabbitmq_node" 
                        (jsonLog { Field "bar" "5672" })
                }
                jsonLog {
                    Field "rabbitmq_node" 
                        (jsonLog { Field "bar" "5672" })
                }
            ]

        let expected =
            "{\n" +
            "    [\n" +
            "        {\n" +
            "            \"rabbitmq_node\": {\n" +
            "                \"bar\": \"5672\"\n" +
            "            }\n" +
            "        },\n" +
            "        {\n" +
            "            \"rabbitmq_node\": {\n" +
            "                \"bar\": \"5672\"\n" +
            "            }\n" +
            "        }\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``toString - two dim json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJson (
                "foo",
                [
                    [TechJsonLogField.ArrayJsonAnnonimous [
                        jsonLog {
                            Field "rabbitmq_node" 
                                (jsonLog { Field "bar" "5672" })
                        }
                    ]]
                ]
            )

        let expected =
            "{\n" +
            "    \"foo\": [\n" +
            "        [\n" +
            "            {\n" +
            "                \"rabbitmq_node\": {\n" +
            "                    \"bar\": \"5672\"\n" +
            "                }\n" +
            "            }\n" +
            "        ]\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``toString - multi two dim json array to string test`` () =
        let json =
            TechJsonLogField.ArrayJson (
                "foo",
                [
                    [TechJsonLogField.ArrayJsonAnnonimous [
                        jsonLog {
                            Field "rabbitmq_node" 
                                (jsonLog { Field "bar" "5672" })
                        }
                        jsonLog {
                            Field "rabbitmq_node" 
                                (jsonLog { Field "bar" "5672" })
                        }
                    ]]
                ]
            )

        let expected =
            "{\n" +
            "    \"foo\": [\n" +
            "        [\n" +
            "            {\n" +
            "                \"rabbitmq_node\": {\n" +
            "                    \"bar\": \"5672\"\n" +
            "                }\n" +
            "            },\n" +
            "            {\n" +
            "                \"rabbitmq_node\": {\n" +
            "                    \"bar\": \"5672\"\n" +
            "                }\n" +
            "            }\n" +
            "        ]\n" +
            "    ]\n" +
            "}"

        let s = TechJsonLogField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected
