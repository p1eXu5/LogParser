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
open LogParser.App.LogRepository
open LogParser.Types

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
            |> List.map (fun l -> l.ToString())
        )
        |> LogSourceText.createUnsafe

    let private logSourceText (logs: TechLog list) =
        let newLine = Environment.NewLine
        String.Join(
            "," + newLine,
            logs
            |> List.map (fun l -> l.ToString())
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
                .BeOfCase(ParseTextRequestResult.Accepted)
        }

    [<Test>]
    let ``GetNextLogBatch. When repo is initialized returns empty`` () =
        async {
            let appConfig = appConfig 2
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId

            let! res = logRepository.GetNextLogBatch ()
            %res
                .TechLogIds
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
            %res
                .TechLogIds
                .Should()
                .HaveLength 2
        }

    [<Test>]
    let ``GetNextFilteredLogBatch. When repo is in parsing state, logs exist, batch less than log count, returns filtered log batch`` () =
        async {
            let appConfig = appConfig 2
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId
            let logs = TechLog.generateLogLevelMessageN 20
            let logText = logSourceText logs
            let filter = [(nameof TechJsonLogField.Level, logs[0] |> TechLog.tryFind TechJsonSpecialFieldType.Level |> _.Value)] |> Map.ofList

            let! _ = logRepository.ParseTextAsync logText
            let! res = logRepository.GetNextFilteredLogBatch filter

            %res
                .TechLogIds
                .Length
                .Should()
                .BeInRange(1, 2)
        }

    [<Test>]
    let ``GetLogs. When repo is in parsing state, logs exist, returns log in correct order`` () =
        async {
            let appConfig = appConfig 3
            let appSubject = appSubject appConfig
            let logSourceId = LogSourceId.create ()
            let logRepository = logRepository appConfig appSubject logSourceId
            let logs = TechLog.generateLogLevelMessageN 3
            let logText = logSourceText logs

            let! _ = logRepository.ParseTextAsync logText
            let! logMetaBatch = logRepository.GetNextLogBatch ()
            let! logBatch = logRepository.GetLogs logMetaBatch.TechLogIds
            %logBatch.TechLogs[0]
                .Should()
                .Be(logs[0])
        }

