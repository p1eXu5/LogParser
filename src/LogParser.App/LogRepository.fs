namespace LogParser.App.LogRepository

open System
open System.Collections.Generic
open System.IO
open System.Threading
open Microsoft.Extensions.Logging
open LogParser.App.Abstractions
open System.Threading.Tasks
open System.Text
open LogParser
open LogParser.App
open LogParser.Types
open p1eXu5.FSharp.Reactive
open Gma.DataStructures.StringSearch
open System.Buffers

type LogRepository =
    {
        ParseTextAsync: LogSourceText -> Async<ParseTextRequestResult>
        ParseFileAsync: FilePath -> Async<ParseTextRequestResult>
        GetNextLogBatch: unit -> Async<LogMetaBatch>
        GetNextFilteredLogBatch: Map<FieldKey, string> -> Async<LogMetaBatch>
        /// Returns Tech and Raw logs asynchronously.
        GetLogs: TechLogId list -> Async<LogBatch>
        GetTechLogs: TechLogId list -> CacheKey -> Map<TechLogId, TechLog>
        GetRawLogs: TechLogId list -> Map<TechLogId, string>
        Dispose: unit -> unit
    }
    interface IDisposable with
        member this.Dispose() =
            this.Dispose()
and
    CacheKey = Guid
and
    ParseTextRequestResult =
        | Accepted of LogSource
        | PreviousInProgress
        | PreviousNotStorred
        | CouldNotOpenUserFile of FilePath
//        | LogRepositoryError of LogRepositoryError
//and
//    LogRepositoryError =
//        | LogSourceItemInitializationError of string
and
    LogMetaBatch =
        {
            ///// Is needed to cache result
            // BatchId: Guid
            TechLogIds: TechLogId list
            /// All known field keys
            FieldKeys: Set<FieldKey>
            ParsingError: bool
        }
and
    LogBatch =
        {
            ///// Is needed to cache result
            //BatchId: Guid
            TechLogs: TechLog list
            RawLogs: string list
        }
and
    LogRepositoryLogger =
        {
            LogInitializingLogSourceItem: LogSourceId -> unit
            LogLogSourceItemInitialized: LogSourceId -> LogSource -> unit
            LogCreateTmpFileError: LogSourceId -> FileStorageError -> unit
            LogOpenTmpFileError: LogSourceId -> exn -> unit
            LogCreateMemoryStreamError: LogSourceId -> exn -> unit
            LogLogStreamParsedSuccessfully: LogSourceId -> unit
            LogLogStreamParsingError: LogSourceId -> string -> unit
            LogLogStreamError: LogSourceId -> exn -> unit
            LogProcessingMsg: string -> string -> unit
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
                    LogLogStreamError = log "Log stream %A has not been parsed - %O"
                    LogProcessingMsg = log "Message %s is processing. State - %s"
                    LogUnprocessedMsg = log "Message %s is skipped. State - %s"
                }

// exception LogParsingException of LogRepositoryError

module private LogMetaBatch =

    let create (techLogIds: TechLogId list, fieldKeys: Set<FieldKey>, parsingError: bool) =
        {
            TechLogIds = techLogIds
            FieldKeys = fieldKeys
            ParsingError = parsingError
        }

module private LogBatch =

    let create (techLogs: TechLog list) (rawLogs: string list) =
        {
            TechLogs = techLogs
            RawLogs = rawLogs
        }

module LogRepository =

    type private State =
        | Initialized of InitializedState
        | Parsing of ParsingState
        | Parsed of ParsedState
    and
        private InitializedState =
            {
                Filter: Map<FieldKey, string>
            }
    and
        private ParsingState =
            {
                Cts: CancellationTokenSource
                ParseTask: Task
                Stream: Stream
                StreamSource: LogSource
                Logs: TechLogPosition list
                Skip: int
                (*
                Special fields could be stored separately,
                but content of these fields is unpredictable

                User search values could be stored near the logs
                *)
                Fields: Map<FieldKey, int> // get field -> get field to value (UkkonenTrie) -> get log list
                FieldValueToLogs: UkkonenTrie<int> list
                Filter: Map<FieldKey, string>
                /// Set when log meta batch was requested
                LogsRequested: bool
            }
            interface IStateLogs with
                member this.FieldValueToLogs = this.FieldValueToLogs
                member this.Fields = this.Fields
                member this.Filter = this.Filter
                member this.Logs = this.Logs
                member this.Skip = this.Skip
            interface IStateLogsStream with
                member this.Logs = this.Logs
                member this.Stream = this.Stream
    and
        private ParsedState =
            {
                Stream: Stream
                StreamSource: LogSource
                Logs: TechLogPosition list
                Skip: int
                Fields: Map<FieldKey, int>
                FieldValueToLogs: UkkonenTrie<int> list
                Filter: Map<FieldKey, string>
                ParsingError: bool
            }
            interface IStateLogs with
                member this.FieldValueToLogs = this.FieldValueToLogs
                member this.Fields = this.Fields
                member this.Filter = this.Filter
                member this.Logs = this.Logs
                member this.Skip = this.Skip
            interface IStateLogsStream with
                member this.Logs = this.Logs
                member this.Stream = this.Stream
    and
        private IStateLogs =
            interface
                abstract Logs: TechLogPosition list with get
                abstract Skip: int with get
                abstract Fields: Map<FieldKey, int> with get
                abstract FieldValueToLogs: UkkonenTrie<int> list with get
                abstract Filter: Map<FieldKey, string> with get
            end
    and
        private IStateLogsStream =
            interface
                abstract Logs: TechLogPosition list with get
                abstract Stream: Stream
            end

    module private State =

        let filter (state: State) =
            match state with
            | State.Initialized s -> s.Filter
            | State.Parsing s -> s.Filter
            | State.Parsed s -> s.Filter

        let name (state: State) =
            match state with
            | State.Initialized s -> nameof State.Initialized
            | State.Parsing s -> nameof State.Parsing
            | State.Parsed s -> nameof State.Parsed

        let (|LogsStream|_|) (state: State) =
            match state with
            | State.Parsing s -> s :> IStateLogsStream |> Some
            | State.Parsed s -> s :> IStateLogsStream |> Some
            | State.Initialized s -> None

    type private Msg =
        | ParseText of LogSourceText * AsyncReplyChannel<ParseTextRequestResult>
        | ParseFile of FilePath * AsyncReplyChannel<ParseTextRequestResult>
        | AppendLogBatch of TechLogPosition seq
        | SetError of exn
        | SetParsedState
        | GetNextBatch of AsyncReplyChannel<LogMetaBatch>
        | GetNextFilteredBatch of filter: Map<FieldKey, string> * AsyncReplyChannel<LogMetaBatch>
        | GetLogs of logIdList: TechLogId list * AsyncReplyChannel<LogBatch>
        | GetTechLogs of logIdList: TechLogId list * AsyncReplyChannel<Map<TechLogId, TechLog>>
        | GetRawLogs of logIdList: TechLogId list * AsyncReplyChannel<Map<TechLogId, string>>


    let [<Literal>] TEXT_LOG_KEY = "{T}"

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

    let private logFileStreamOfText
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
            , LogSource.MemoryStream

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
                            , LogSource.TempFile filePath
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

    let private logFileStreamOfFile
        (logger: LogRepositoryLogger)
        (fileStorage: FileStorage)
        (logSourceId: LogSourceId)
        (ct: CancellationToken)
        (filePath: FilePath)
        =
        do logger.LogInitializingLogSourceItem logSourceId

        try
            let (stream, source) =
                (
                    new CancellableStream(
                        File.Open(filePath |> FilePath.value, FileMode.Open, FileAccess.ReadWrite),
                        ct
                    ) :> Stream
                    , LogSource.UserFile filePath
                )

            do logger.LogLogSourceItemInitialized logSourceId (source)

            (stream, source) |> Ok
        with ex ->
            logger.LogOpenTmpFileError logSourceId ex
            Error ()

    let private logs (appConfig: AppConfig) (stateLogs: IStateLogs) =
        let skip = stateLogs.Skip
        if skip >= stateLogs.Logs.Length - 1 then
            List.empty
        else
            let length = stateLogs.Logs.Length
            if stateLogs.Filter |> Map.isEmpty then
                stateLogs.Logs
                |> List.rev
                |> List.skip skip
                |> List.take appConfig.ParserSubscriptionBatchSize
                |> List.mapi (fun ind l -> TechLogId.fromTechLogPosition (length - skip - ind - 1) length l)
            else
                stateLogs.Fields
                |> Map.fold (fun s key ind ->
                    match stateLogs.Filter |> Map.tryFind key with
                    | Some v ->
                        s |> Seq.append (stateLogs.FieldValueToLogs[ind].Retrieve(v))
                    | None -> s
                ) Seq.empty
                |> Seq.sortDescending
                |> Seq.skip skip
                |> Seq.take appConfig.ParserSubscriptionBatchSize
                |> Seq.map (fun ind ->
                    stateLogs.Logs[length - ind - 1] |> TechLogId.fromTechLogPosition ind length
                )
                |> Seq.toList


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
                        // get prioritized message
                        let! msg =
                            async {
                                let! msgOpt =
                                    processor.TryScan(
                                        fun m ->
                                            match m with
                                            | Msg.GetTechLogs _ -> async.Return m |> Some
                                            | _ -> None
                                        , 0
                                    )

                                match msgOpt with
                                | Some m -> return m
                                | None ->
                                    match state with
                                    | State.Parsing s when s.LogsRequested ->
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
                                        
                                    | _ -> return! processor.Receive()
                        }

                        // log message:
                        logger.LogProcessingMsg (msg |> sprintf "%A") (state |> State.name) 

                        // handling:
                        match msg with
                        | Msg.ParseText (logSourceText, reply) ->
                            match state with
                            | Parsing _ -> reply.Reply (ParseTextRequestResult.PreviousInProgress)
                            | Parsed s when s.StreamSource |> LogFile.isNotUserFile -> reply.Reply (ParseTextRequestResult.PreviousInProgress)
                            // msg can be an Initialized or a Parsed with saved user file
                            | _ ->
                                let cts = new CancellationTokenSource()
                                let! streamLogFile = logSourceText |> logFileStreamOfText logger fileStorage logSourceId cts.Token
                                let (stream, logFile) = streamLogFile
                                
                                reply.Reply(ParseTextRequestResult.Accepted logFile)

                                let observer = appSubject.GetObserver logSourceId
                                let parsingState =
                                    {
                                        Cts = cts
                                        Stream = stream
                                        StreamSource = logFile
                                        ParseTask = stream |> parseTask logger logSourceId observer cts.Token
                                        Logs = []
                                        Skip = 0
                                        Fields = seq { (TEXT_LOG_KEY, 0) } |> Map.ofSeq 
                                        FieldValueToLogs = [ UkkonenTrie<int>() ]
                                        Filter = state |> State.filter
                                        LogsRequested = false
                                    }
                                    |> State.Parsing

                                return! running parsingState

                        | Msg.ParseFile (filePath, reply) ->
                            match state with
                            | Parsing _ -> reply.Reply (ParseTextRequestResult.PreviousInProgress)
                            | Parsed s when s.StreamSource |> LogFile.isNotUserFile -> reply.Reply (ParseTextRequestResult.PreviousInProgress)
                            // msg can be an Initialized or a Parsed with saved user file
                            | _ ->
                                let cts = new CancellationTokenSource()
                                match filePath |> logFileStreamOfFile logger fileStorage logSourceId cts.Token with
                                | Ok streamLogFile ->
                                    let (stream, logFile) = streamLogFile
                                
                                    reply.Reply(ParseTextRequestResult.Accepted logFile)

                                    let observer = appSubject.GetObserver logSourceId
                                    let parsingState =
                                        {
                                            Cts = cts
                                            Stream = stream
                                            StreamSource = logFile
                                            ParseTask = stream |> parseTask logger logSourceId observer cts.Token
                                            Logs = []
                                            Skip = 0
                                            Fields = seq { (TEXT_LOG_KEY, 0) } |> Map.ofSeq 
                                            FieldValueToLogs = [ UkkonenTrie<int>() ]
                                            Filter = state |> State.filter
                                            LogsRequested = false
                                        }
                                        |> State.Parsing

                                    return! running parsingState
                                | Error _ ->
                                    reply.Reply (ParseTextRequestResult.CouldNotOpenUserFile filePath)

                        | Msg.AppendLogBatch logs ->
                            match state with
                            | State.Parsing s -> 
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
                                    { s with Logs = stateLogs; Fields = stateFields; FieldValueToLogs = stateFieldValueToLogs; LogsRequested = false }
                                    |> State.Parsing

                                return! running newState
                            | _ ->
                                logger.LogUnprocessedMsg "AppendLogBatch" (state |> State.name)
                                return! running state

                        | Msg.GetNextBatch reply ->
                            match state with
                            | State.Initialized _ ->
                                reply.Reply (([], Set.empty, false) |> LogMetaBatch.create)
                                return! running state
                            | State.Parsed s ->
                                let logs = logs appConfig s
                                reply.Reply ((logs, s.Fields.Keys |> Set.ofSeq, s.ParsingError) |> LogMetaBatch.create)
                                return! running ({ s with Skip = s.Skip + logs.Length } |> State.Parsed)
                            | State.Parsing s ->
                                let logs = logs appConfig s

                                if logs.Length = 0
                                then
                                    let s = { s with LogsRequested = true }
                                    processor.Post(Msg.GetNextBatch reply)
                                    return! running state
                                else 
                                    reply.Reply ((logs, s.Fields.Keys |> Set.ofSeq, false) |> LogMetaBatch.create)
                                    return! running ({ s with Skip = s.Skip + logs.Length } |> State.Parsing)

                        | Msg.GetNextFilteredBatch (filter, reply) ->
                            match state with
                            | State.Initialized s ->
                                reply.Reply (([], Set.empty, false) |> LogMetaBatch.create)
                                return! running ({ s with Filter = filter } |> Initialized)
                            | State.Parsed s ->
                                let s =
                                    if filter = s.Filter then s else { s with Filter = filter }

                                let logs = logs appConfig s
                                reply.Reply ((logs, s.Fields.Keys |> Set.ofSeq, s.ParsingError) |> LogMetaBatch.create)
                                return! running ({ s with Skip = s.Skip + logs.Length } |> State.Parsed)
                            | State.Parsing s ->
                                let s =
                                    if filter = s.Filter then s else { s with Filter = filter }

                                let logs = logs appConfig s

                                if logs.Length = 0
                                then
                                    let s = { s with LogsRequested = true }
                                    processor.Post(Msg.GetNextFilteredBatch (filter, reply))
                                    return! running state
                                else 
                                    reply.Reply ((logs, s.Fields.Keys |> Set.ofSeq, false) |> LogMetaBatch.create)
                                    return! running ({ s with Skip = s.Skip + logs.Length } |> State.Parsing)

                        | Msg.SetParsedState ->
                            match state with
                            | State.Initialized _ ->
                                return! running state
                            | State.Parsed _ ->
                                return! running state
                            | State.Parsing s ->
                                let parsedState =
                                    {
                                        Stream = s.Stream
                                        StreamSource = s.StreamSource
                                        Logs = s.Logs
                                        Skip = s.Skip
                                        Fields = s.Fields
                                        FieldValueToLogs = s.FieldValueToLogs
                                        Filter = s.Filter
                                        ParsingError = false
                                    }

                                return! running (parsedState |> State.Parsed)

                        | Msg.SetError ex ->
                            logger.LogLogStreamError logSourceId ex
                            match state with
                            | State.Parsing s ->
                                let parsedState =
                                    {
                                        Stream = s.Stream
                                        StreamSource = s.StreamSource
                                        Logs = s.Logs
                                        Skip = s.Skip
                                        Fields = s.Fields
                                        FieldValueToLogs = s.FieldValueToLogs
                                        Filter = s.Filter
                                        ParsingError = true
                                    }

                                return! running (parsedState |> State.Parsed)

                            | _ ->
                                logger.LogUnprocessedMsg (nameof Msg.SetError) (state |> State.name)
                                return! running state

                        | Msg.GetLogs (logIdList, reply) ->
                            if logIdList.Length = 0 then
                                reply.Reply (LogBatch.create [] [])
                                return! running state
                            else
                                match state with
                                | State.LogsStream s ->
                                    let mutable buf = ArrayPool<char>.Shared.Rent(int (logIdList[0].EndIndex - logIdList[0].StartIndex))
                                    use sr = new StreamReader(s.Stream, leaveOpen = true)
                                    let logBatch =
                                        logIdList
                                        |> List.fold
                                            (fun batch logId ->
                                                let count = int (logId.EndIndex - logId.StartIndex)
                                                if count > buf.Length then
                                                    ArrayPool<char>.Shared.Return(buf);
                                                    buf <- ArrayPool<char>.Shared.Rent(count)
                                                let _ = sr.BaseStream.Seek(logId.StartIndex, SeekOrigin.Begin)
                                                let read = sr.ReadBlock(buf, 0, count)
                                                { batch with
                                                    TechLogs = s.Logs[logId.Ind].Log :: batch.TechLogs
                                                    RawLogs = System.String(buf, 0, read) :: batch.RawLogs
                                                }
                                            )
                                            ({ TechLogs = []; RawLogs = [] })
                                    sr.Dispose()
                                    ArrayPool<char>.Shared.Return(buf);
                                    reply.Reply (logBatch)
                                    return! running state
                                | _ ->
                                    reply.Reply (LogBatch.create [] [])
                                    return! running state

                        | Msg.GetTechLogs (logIdList, reply) ->
                            if logIdList.Length = 0 then
                                reply.Reply (Map.empty)
                                return! running state
                            else
                                match state with
                                | State.LogsStream s ->
                                    let logBatch =
                                        logIdList
                                        |> Seq.map (fun logId ->
                                            (logId, s.Logs[logId.Ind].Log)
                                        )
                                        |> Map.ofSeq

                                    reply.Reply (logBatch)
                                    return! running state
                                | _ ->
                                    reply.Reply (Map.empty)
                                    return! running state

                        | Msg.GetRawLogs (logIdList, reply) ->
                            if logIdList.Length = 0 then
                                reply.Reply (Map.empty)
                                return! running state
                            else
                                match state with
                                | State.LogsStream s ->
                                    let mutable buf = ArrayPool<char>.Shared.Rent(int (logIdList[0].EndIndex - logIdList[0].StartIndex))
                                    use sr = new StreamReader(s.Stream, leaveOpen = true)
                                    let logBatch =
                                        logIdList
                                        |> Seq.map
                                            (fun logId ->
                                                let count = int (logId.EndIndex - logId.StartIndex)
                                                if count > buf.Length then
                                                    ArrayPool<char>.Shared.Return(buf);
                                                    buf <- ArrayPool<char>.Shared.Rent(count)
                                                let _ = sr.BaseStream.Seek(logId.StartIndex, SeekOrigin.Begin)
                                                let read = sr.ReadBlock(buf, 0, count)
                                                (logId, System.String(buf, 0, read))
                                            )
                                        |> Map.ofSeq
                                    sr.Dispose()
                                    ArrayPool<char>.Shared.Return(buf);
                                    reply.Reply (logBatch)
                                    return! running state
                                | _ ->
                                    reply.Reply (Map.empty)
                                    return! running state
                    }

                running (Initialized { Filter = Map.empty })
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

        let mutable (_cacheKey: CacheKey) = Guid.Empty
        let mutable _techLogs = Map.empty

        {
            ParseTextAsync = fun (logSourceText: LogSourceText) ->
                agent.PostAndAsyncReply(fun reply -> Msg.ParseText (logSourceText, reply))
            ParseFileAsync = fun (logFile: FilePath) ->
                agent.PostAndAsyncReply(fun reply -> Msg.ParseFile (logFile, reply))
            GetNextLogBatch = fun () ->
                agent.PostAndAsyncReply(fun reply -> Msg.GetNextBatch reply)
            GetNextFilteredLogBatch = fun filter ->
                agent.PostAndAsyncReply(fun reply -> Msg.GetNextFilteredBatch (filter, reply))
            GetLogs = fun logIdList ->
                agent.PostAndAsyncReply(fun reply -> Msg.GetLogs (logIdList, reply))
            GetTechLogs= fun logIdList cacheKey ->
                if cacheKey = _cacheKey then _techLogs
                else
                    let techLogs = agent.PostAndReply(fun reply -> Msg.GetTechLogs (logIdList, reply))
                    _cacheKey <- cacheKey
                    _techLogs <- techLogs
                    techLogs
            GetRawLogs= fun logIdList ->
                agent.PostAndReply(fun reply -> Msg.GetRawLogs (logIdList, reply))
            Dispose = fun () ->
                subscription.Dispose()
                agent.Dispose()
        }
