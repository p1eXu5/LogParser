namespace LogParser.App

open System
open System.Collections.Generic
open System.IO
open System.Threading
open Microsoft.Extensions.Logging
open LogParser.App.Abstractions
open System.Threading.Tasks
open System.Text
open LogParser
open LogParser.Types
open p1eXu5.FSharp.Reactive
open Gma.DataStructures.StringSearch

type LogRepository =
    {
        ParseTextAsync: LogSourceText -> Async<LogRepositoryParseTextRequestResult>
        GetNextLogBatch: unit -> Async<(TechLogId list * Set<FieldKey>)>
        Dispose: unit -> unit
    }
    interface IDisposable with
        member this.Dispose() =
            this.Dispose()
and
    LogRepositoryConfiguration =
        {
            ParserSubscriptionBatchSize: int
        }
and
    LogRepositoryParseTextRequestResult =
        | Accepted of LogFile
        | PreviousInProgress
        | PreviousNotStorred
//        | LogRepositoryError of LogRepositoryError
//and
//    LogRepositoryError =
//        | LogSourceItemInitializationError of string
and
    LogRepositoryLogger =
        {
            LogInitializingLogSourceItem: LogSourceId -> unit
            LogLogSourceItemInitialized: LogSourceId -> LogFile -> unit
            LogCreateTmpFileError: LogSourceId -> FileStorageError -> unit
            LogOpenTmpFileError: LogSourceId -> exn -> unit
            LogCreateMemoryStreamError: LogSourceId -> exn -> unit
            LogLogStreamParsedSuccessfully: LogSourceId -> unit
            LogLogStreamParsingError: LogSourceId -> string -> unit
            LogUnprocessedMsg: string -> string -> unit
        }
        with
            static member Console =
                let now () = DateTimeOffset.Now.ToString("HH':'mm':'ss.fffff")
                let locker = Lock()

                let log fmt =
                    Printf.kprintf (fun msg ->
                        lock locker (fun () ->
                            printfn "[%s] LogParser.App.LogRepository\n\t%s (Thread #%i)."
                                (now ())
                                msg
                                Thread.CurrentThread.ManagedThreadId)
                    ) fmt
                {
                    LogInitializingLogSourceItem = log "Initializing log source item: %A"
                    LogLogSourceItemInitialized = log "Log source item has been initialized: %A. Log file - %A."
                    LogCreateTmpFileError = log "Failed to create temp file for %A - %A. Initializing memory stream..."
                    LogOpenTmpFileError = log "Failed to open temp file for %A - %A. Initializing memory stream..."
                    LogCreateMemoryStreamError = log "Failed to create memory stream for %A - %A"
                    LogLogStreamParsedSuccessfully = log "Log stream %A has been parsed successfully"
                    LogLogStreamParsingError = log "Log stream %A has not been parsed - %s"
                    LogUnprocessedMsg = log "Message %s is skipped. State - %s"
                }

// exception LogParsingException of LogRepositoryError

module LogRepository =

    type private State =
        | Initialized
        | Parsing of ParsingState
        | Parsed of ParsedState
    and
        private ParsingState =
            {
                Cts: CancellationTokenSource
                Stream: Stream
                StreamSource: LogFile
                ParseTask: Task
                Logs: TechLogPosition list
                LastRequestedPage: int
                (*
                Special fields could be stored separately,
                but content of these fields is unpredictable

                User search values could be stored near the logs
                *)
                Fields: Map<FieldKey, int>
                FieldValueToLogs: UkkonenTrie<int> list
            }
    and
        ParsedState =
            {
                Id: LogSourceId
                Stream: Stream
                StreamSource: LogFile
                Logs: TechLogPosition list
                Cursor: int
                Fields: Map<FieldKey, int>
                FieldValueToLogs: UkkonenTrie<int> list
            }

    type private Msg =
        | ParseText of LogSourceText * AsyncReplyChannel<LogRepositoryParseTextRequestResult>
        | AppendLogBatch of TechLogPosition seq
        | SetError of exn
        | SetParsedState
        | GetNextBatch of AsyncReplyChannel<(TechLogId list * Set<FieldKey>)>

    let [<Literal>] TEXT_LOG_KEY = "{T}"

    module private MsgWith =
        let (|AppendLogBatch|_|) (state: State) (msg: Msg) =
            match msg with
            | Msg.AppendLogBatch logs ->
                match state with
                | State.Parsing s -> Some (s, logs)
                | _ -> None
            | _ -> None


    let private parseTask (logger: LogRepositoryLogger) (logSourceId: LogSourceId) (observer: IObserver<TechLogPosition>) (ct: CancellationToken) (stream: Stream) =
        let streamName = sprintf "%O" logSourceId
        Task.Factory.StartNew(
            Action (fun () ->
                              
                let parseResult = TechLogParser.parseStream observer streamName stream
                match parseResult with
                | Ok () -> logger.LogLogStreamParsedSuccessfully logSourceId
                | Error err -> logger.LogLogStreamParsingError logSourceId err
            ),
            ct,
            TaskCreationOptions.PreferFairness ||| TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        )

    let private logFileStream
        (logger: LogRepositoryLogger)
        (fileStorage: FileStorage)
        (logSourceId: LogSourceId)
        (ct: CancellationToken)
        (text: LogSourceText)
        =
        let createMemoryStream () =
            new CancellableStream(
                new MemoryStream(Encoding.UTF8.GetBytes(text.Value)) :> Stream,
                ct
            ) :> Stream
            , LogFile.MemoryStream

        async {
            do logger.LogInitializingLogSourceItem logSourceId

            let! result = 
                fileStorage.CreateTmpFileTask text ct
                |> Async.AwaitTask

            let streamResult =
                match result with
                | Ok filePath ->
                    try
                        (
                            new CancellableStream(
                                File.Open(filePath |> FilePath.value, FileMode.Open, FileAccess.ReadWrite),
                                ct
                            ) :> Stream
                            , LogFile.TempFile filePath
                        )
                    with ex ->
                        logger.LogOpenTmpFileError logSourceId ex
                        createMemoryStream ()

                | Error err ->
                    logger.LogCreateTmpFileError logSourceId err
                    createMemoryStream ()

            do logger.LogLogSourceItemInitialized logSourceId (snd streamResult)

            return
                streamResult
        }

    let private agent
        (appConfig: AppConfig)
        (appSubject: AppSubject)
        (fileStorage: FileStorage)
        (logger: LogRepositoryLogger)
        (logSourceId: LogSourceId)
        =
        new MailboxProcessor<Msg>(
            (fun processor ->
                let rec running (state: State) =
                    async {
                        let! msg =
                            match state with
                            | State.Parsing _ ->
                                async {
                                    let! msgOpt =
                                        processor.TryScan(
                                            fun m ->
                                                match m with
                                                | Msg.AppendLogBatch _ -> async.Return m |> Some
                                                | _ -> None
                                            , 0
                                        )
                                    return!
                                        match msgOpt with
                                        | None -> processor.Receive()
                                        | Some m -> async.Return m
                                }
                            | _ -> processor.Receive()

                        match msg with
                        | Msg.ParseText (logSourceText, reply) ->
                            match state with
                            | Parsing _ -> reply.Reply (LogRepositoryParseTextRequestResult.PreviousInProgress)
                            | Parsed s when s.StreamSource |> LogFile.isNotUserFile -> reply.Reply (LogRepositoryParseTextRequestResult.PreviousInProgress)
                            // msg can be an Initialized or a Parsed with saved user file
                            | _ ->
                                let cts = new CancellationTokenSource()
                                let! streamLogFile = logSourceText |> logFileStream logger fileStorage logSourceId cts.Token
                                let (stream, logFile) = streamLogFile
                                
                                reply.Reply(LogRepositoryParseTextRequestResult.Accepted logFile)

                                let observer = appSubject.GetObserver logSourceId
                                let parsingState =
                                    {
                                        Cts = cts
                                        Stream = stream
                                        StreamSource = logFile
                                        ParseTask = stream |> parseTask logger logSourceId observer cts.Token
                                        Logs = []
                                        LastRequestedPage = 0
                                        Fields = seq { (TEXT_LOG_KEY, 0) } |> Map.ofSeq 
                                        FieldValueToLogs = [ UkkonenTrie<int>() ]
                                    }
                                    |> State.Parsing

                                return! running parsingState

                        | MsgWith.AppendLogBatch state (s, logs) ->
                            let (stateLogs, stateFields, stateFieldValueToLogs) =
                                Seq.foldBack
                                    (fun (log: TechLogPosition) (logs: TechLogPosition list, fields: Map<FieldKey, int>, fieldValueToLogs: UkkonenTrie<int> list) ->
                                        match log.Log with
                                        | TechLog.TextLog text ->
                                            fieldValueToLogs[0].Add(text, logs.Length)
                                            (log :: logs, fields, fieldValueToLogs)
                                        | TechLog.JsonLog json ->
                                            json.Fields
                                            |> List.fold
                                                (fun (fields: Map<FieldKey, int>, fieldValueToLogs: UkkonenTrie<int> list) field ->
                                                    let key = field |> TechJsonLogField.key
                                                
                                                    let (fields, ind, fieldValueToLogs) =
                                                        match fields |> Map.tryFind key with
                                                        | Some ind -> (fields, ind, fieldValueToLogs)
                                                        | None ->
                                                            (fields |> Map.add key fieldValueToLogs.Length, fieldValueToLogs.Length, UkkonenTrie<int>() :: fieldValueToLogs)

                                                    fieldValueToLogs[ind].Add(field |> TechJsonLogField.value, logs.Length)

                                                    (fields, fieldValueToLogs)
                                                )
                                                (fields, fieldValueToLogs)
                                            |> fun (fields, fieldValueToLogs) ->
                                                (log :: logs, fields, fieldValueToLogs)
                                    )
                                    logs
                                    (s.Logs, s.Fields, s.FieldValueToLogs)

                            let newState =
                                { s with Logs = stateLogs; Fields = stateFields; FieldValueToLogs = stateFieldValueToLogs }
                                |> State.Parsing

                            return! running newState

                        | Msg.GetNextBatch reply ->
                            match state with
                            | State.Initialized -> reply.Reply ([], Set.empty)
                            | State.Parsed s ->
                                let skip = s.Cursor * appConfig.ParserSubscriptionBatchSize
                                if skip >= s.Logs.Length - 1 then
                                    reply.Reply ([], Set.empty)
                                    return! running state
                                else
                                    s.Logs
                                    |> List.skip skip
                                    |> List.take appConfig.ParserSubscriptionBatchSize
                                    |> List.map TechLogId.fromTechLogPosition
                                    |> fun ids ->
                                        reply.Reply((ids, s.Fields.Keys |> Set.ofSeq))
                                    return! running ({ s with Cursor = s.Cursor + 1 } |> State.Parsed)

                            | State.Parsing s ->
                                let skip = s.LastRequestedPage * appConfig.ParserSubscriptionBatchSize
                                if skip >= s.Logs.Length - 1 then
                                    processor.Post(Msg.GetNextBatch reply)
                                    return! running state
                                else
                                    s.Logs
                                    |> List.skip skip
                                    |> List.take appConfig.ParserSubscriptionBatchSize
                                    |> List.map TechLogId.fromTechLogPosition
                                    |> fun ids ->
                                        reply.Reply((ids, s.Fields.Keys |> Set.ofSeq))
                                    return! running ({ s with LastRequestedPage = s.LastRequestedPage + 1 } |> State.Parsing)

                        | msg ->
                            logger.LogUnprocessedMsg (msg |> sprintf "%O") (state |> sprintf "%O")
                            return! running state
                    }

                running Initialized
            )
        )

    let init (appConfig: AppConfig) (appSubject: AppSubject) (fileStorage: FileStorage) (logger: LogRepositoryLogger) (logSourceId: LogSourceId) =
        let agent = agent appConfig appSubject fileStorage logger logSourceId

        let subscription =
            appSubject
            :> IObservable<LogSourceId * TechLogPosition seq>
            |> Observable.filter (fun (id, _) -> id = logSourceId)
            |> Observable.subscribeWithCallbacks
                (fun (_, l) -> agent.Post(Msg.AppendLogBatch(l)))
                (fun ex -> agent.Post(Msg.SetError ex))
                (fun () -> agent.Post(Msg.SetParsedState))

        agent.Start()

        {
            ParseTextAsync = fun (logSourceText: LogSourceText) ->
                agent.PostAndAsyncReply(fun reply -> Msg.ParseText (logSourceText, reply))
            GetNextLogBatch = fun () ->
                agent.PostAndAsyncReply(fun reply -> Msg.GetNextBatch reply)
            Dispose = fun () ->
                subscription.Dispose()
                agent.Dispose()
        }
