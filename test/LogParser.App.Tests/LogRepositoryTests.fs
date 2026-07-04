namespace LogParser.App.Tests

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging

open NUnit.Framework
open Faqt
open Faqt.Operators
open p1eXu5.FSharp.Reactive
open p1eXu5.FSharp.Reactive.Testing
open p1eXu5.AspNetCore.Testing.Logging
open LogParser.Tests.Fakers


open LogParser.App

module LogRepositoryTests =

    let private appSubject (parserSubscriptionBatchSize: int) =
        let appConfig =
            {
                ParserSubscriptionBatchSize = parserSubscriptionBatchSize
                ParserBatchFlushTimeSpan = TimeSpan.FromMilliseconds(200.0)
            }
        AppSubject.init AppSubjectLogger.Console appConfig

    let private logRepository appSubject =
        // To force use memory stream
        let erroredFileStorage : FileStorage =
            {
                CreateTmpFileTask = fun _ _ -> Task.FromResult (Error FileStorageError.TmpFileCreatingError)
            }
        LogRepository.init appSubject erroredFileStorage LogRepositoryLogger.Console

    let private logText () =
        let newLine = Environment.NewLine
        String.Join(
            newLine,
            TechLog.generateJsonMany ()
            |> List.map (fun l -> $"{{{newLine}{l.ToString()}{newLine}}}")
        )
        |> LogSourceText.createUnsafe

    [<Test>]
    let ``ParseText. When text is valid json Then fulfilled item`` () =
        let appSubject = appSubject 2
        let logRepository = logRepository appSubject
        let logText = logText ()
        let logSourceId = LogSourceId.create ()

        let res = logRepository.ParseText logSourceId logText
        %res
            .Should()
            .BeOfCase(Accepted)