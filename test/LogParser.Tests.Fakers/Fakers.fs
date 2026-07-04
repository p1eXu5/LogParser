namespace LogParser.Tests.Fakers

open System
open System.Globalization
open System.Net
open Microsoft.Extensions.Logging
open FParsec
open LogParser.Types

module Position =
    let generate () =
        Position(
            faker.Name.JobTitle(),
            faker.Random.Long(1, 1000), 
            faker.Random.Long(1, 1000), 
            faker.Random.Long(1, 200)
        )

module Timespan =
    let generate () =
        faker.Random.ArrayElement(
            [|
                fun () -> faker.Date.PastOffset().ToString("u", CultureInfo.CurrentUICulture) |> Timespan.Value
                fun () -> Timespan.Null
            |]
        )()

module TechJsonLogField =
    let private primitiveGenerators = 
        let key () =
            faker.Hacker.Noun()

        let list f =
            seq { 1 .. faker.Random.Int(0, 3) }
            |> Seq.map (fun _ -> f ())
            |> Seq.toList

        [|
            Timespan.generate >> TechJsonLogField.Timespan
            fun () -> faker.Lorem.Sentence() |> TechJsonLogField.Message
            fun () -> faker.Random.Enum<LogLevel>() |> TechJsonLogField.Level
            fun () -> faker.Lorem.Word() |> TechJsonLogField.Method
            fun () -> faker.Random.Enum<HttpStatusCode>() |> TechJsonLogField.StatusCode
            fun () -> faker.Internet.UrlWithPath() |> TechJsonLogField.Path
            fun () -> faker.Internet.DomainName() |> TechJsonLogField.Host
            fun () -> faker.Internet.Port() |> TechJsonLogField.Port
            fun () -> (key (), faker.Lorem.Word()) |> TechJsonLogField.String
            fun () -> (key (), faker.Random.Int()) |> TechJsonLogField.Int
            fun () -> (key (), faker.Random.Bool()) |> TechJsonLogField.Bool
            fun () -> (key (), faker.Random.Word |> list) |> TechJsonLogField.Array
            fun () -> (key (), faker.Random.Int |> list) |> TechJsonLogField.ArrayInt
            fun () -> key () |> TechJsonLogField.Null
            // TODO: add other
        |]

    let private complexGenerators =
        let generatePrimitiveMany () =
            seq { 1 .. faker.Random.Int(0, 3) }
            |> Seq.map (fun _ ->
                faker.Random.ArrayElement(
                    [|
                        yield! primitiveGenerators
                    |]
                )())
            |> Seq.toList

        [|
            fun () ->
                (
                    faker.Lorem.Sentence(),
                    generatePrimitiveMany ()
                )
                |> TechJsonLogField.MessageBoddied

            fun () ->
                (
                    faker.Lorem.Sentence(),
                    generatePrimitiveMany (),
                    faker.Lorem.Sentence()
                )
                |> TechJsonLogField.MessageBoddiedWithPostfix

            fun () -> 
                seq { 1 .. faker.Random.Int(0, 3) }
                |> Seq.map (fun _ -> generatePrimitiveMany ())
                |> Seq.toList
                |> TechJsonLogField.MessageArrayJson

            fun () ->
                generatePrimitiveMany () |> TechJsonLogField.Body
        |]

    let generatePrimitive () =
        faker.Random.ArrayElement(
            [|
                yield! primitiveGenerators
            |]
        )()

    let generate () =
        faker.Random.ArrayElement(
            [|
                yield! primitiveGenerators
                yield! complexGenerators
            |]
        )()

    let generateMany () =
        seq { 0 .. faker.Random.Int(1, 3) }
        |> Seq.map (fun _ -> generate ())
        |> Seq.toList

    let generatePrimitiveMany () =
        seq { 0 .. faker.Random.Int(1, 3) }
        |> Seq.map (fun _ -> generatePrimitive ())
        |> Seq.toList


module TechLog =
    let generateText () =
        faker.Lorem.Sentence() |> TechLog.TextLog

    let generateJson () =
        {
            Source =
                faker.Random.ArrayElement(
                    [|
                        fun () -> None
                        fun () -> TechJsonLogField.generatePrimitive () |> Some
                    |]
                )()
            Fields = TechJsonLogField.generateMany ()
        }
        |> TechLog.JsonLog

    let generateJsonMany () =
        seq { 0 .. faker.Random.Int(1, 3) }
        |> Seq.map (fun _ -> generateJson ())
        |> Seq.toList

    let generate () =
        faker.Random.ArrayElement(
            [|
                generateText
                generateJson
            |]
        )()

module LogPosition =
    let generate () =
        {
            Start = Position.generate ()
            End = Position.generate ()
            Log = TechLog.generate ()
        }