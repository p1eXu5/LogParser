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
    let private appConfig (parserSubscriptionBatchSize: int) =
        {
            ParserSubscriptionBatchSize = parserSubscriptionBatchSize
            ParserBatchFlushTimeSpan = TimeSpan.FromMilliseconds(200.0)
        }

    let private appSubject (appConfig: AppConfig) =
        AppSubject.init AppSubjectLogger.Console appConfig

    let private logRepository (appConfig: AppConfig) appSubject logSourcceId =
        // To force use memory stream
        let erroredFileStorage : FileStorage =
            {
                CreateTmpFileTask = fun _ _ -> Task.FromResult (Error FileStorageError.TmpFileCreatingError)
            }
        LogRepository.init appConfig appSubject erroredFileStorage LogRepositoryLogger.Console logSourcceId

    let private generateLogText () =
        let newLine = Environment.NewLine
        String.Join(
            "," + newLine,
            TechLog.generateJsonFrom2To5 ()
            |> List.map (fun l -> $"{{{newLine}{l.ToString()}{newLine}}}")
        )
        |> LogSourceText.createUnsafe

    [<Test>]
    let ``ParseTextAsync. When repo is initialized returns Accepted`` () =
        async {
            let appConfig = appConfig 2
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId
            let logText = generateLogText ()

            let! res = logRepository.ParseTextAsync logText
            %res
                .Should()
                .BeOfCase(LogRepositoryParseTextRequestResult.Accepted)
        }

    [<Test>]
    let ``GetNextLogBatch. When repo is initialized returns empty`` () =
        async {
            let appConfig = appConfig 2
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId

            let! res = logRepository.GetNextLogBatch ()
            %(fst res)
                .Should()
                .BeEmpty()
        }

    [<Test>]
    let ``GetNextLogBatch. When repo is in parsing state, logs exist, returns log batch`` () =
        async {
            let appConfig = appConfig 2
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId
            let logText = generateLogText ()

            let! _ = logRepository.ParseTextAsync logText
            let! res = logRepository.GetNextLogBatch ()
            %(fst res)
                .Should()
                .HaveLength 2
        }

