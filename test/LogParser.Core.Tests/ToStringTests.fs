namespace LogParser.Core.Tests

open LogParser.Core.Types
open NUnit.Framework
open LogParser.Core.Tests.ShouldExtensions

[<Category("ToStringTests")>]
module ToStringTests =

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

        TechField.toString 1 json
        |> should equal expected


    [<Test>]
    let ``json array to string test`` () =
        let json =
            TechField.ArrayJson (
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected

    [<Test>]
    let ``multi json array to string test`` () =
        let json =
            TechField.ArrayJson (
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``annonimous json array to string test`` () =
        let json =
            TechField.ArrayJsonAnnonimous [
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``multi annonimous json array to string test`` () =
        let json =
            TechField.ArrayJsonAnnonimous [
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``two dim json array to string test`` () =
        let json =
            TechField.ArrayJson (
                "foo",
                [
                    [TechField.ArrayJsonAnnonimous [
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected


    [<Test>]
    let ``multi two dim json array to string test`` () =
        let json =
            TechField.ArrayJson (
                "foo",
                [
                    [TechField.ArrayJsonAnnonimous [
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

        let s = TechField.toString 1 [json]
        TestContext.WriteLine(s)

        s |> should equal expected
