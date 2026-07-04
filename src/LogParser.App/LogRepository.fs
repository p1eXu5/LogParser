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

type LogRepository =
    {
        ParseText: LogSourceId -> LogSourceText -> LogRepositoryParseTextRequestResult
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
        | Accepted
        | PreviousInProgress
        | PreviousNotStorred
and
    LogRepositoryError =
        | LogSourceItemInitializationError of LogSourceId
and
    LogRepositoryLogger =
        {
            LogInitializingLogSourceItem: LogSourceId -> unit
            LogLogSourceItemInitialized: LogSourceId -> unit
            LogCreateTmpFileError: LogSourceId -> FileStorageError -> unit
            LogOpenTmpFileError: LogSourceId -> exn -> unit
            LogCreateMemoryStreamError: LogSourceId -> exn -> unit
            LogLogStreamParsedSuccessfully: LogSourceId -> unit
            LogLogStreamParsingError: LogSourceId -> string -> unit
        }
        with
            static member Console =
                let now () = DateTimeOffset.Now.ToString("HH':'mm':'ss.fffff")
                let locker = Lock()

                let log fmt =
                    Printf.kprintf (fun msg ->
                        lock locker (fun () ->
                            printfn "[%s] LogParser.App.AppSubject\n\t%s (Thread #%i)."
                                (now ())
                                msg
                                Thread.CurrentThread.ManagedThreadId)
                    ) fmt
                {
                    LogInitializingLogSourceItem = log "Initializing log source item: %A"
                    LogLogSourceItemInitialized = log "Log source item has been initialized: %A"
                    LogCreateTmpFileError = log "Failed to create temp file for %A - %A"
                    LogOpenTmpFileError = log "Failed to open temp file for %A - %A"
                    LogCreateMemoryStreamError = log "Failed to create memory stream for %A - %A"
                    LogLogStreamParsedSuccessfully = log "Log stream %A has been parsed successfully"
                    LogLogStreamParsingError = log "Log stream %A has not been parsed - %s"
                }

exception LogParsingException of LogRepositoryError

module LogRepository =


    type private LogSource =
        {
            Id: LogSourceId
            Cts: CancellationTokenSource
            Stream: Stream
            StreamSource: LogStreamSource
            ParseTask: Task
            Logs: LogPosition list
        }

    type private LogSourceState =
        | Initializing of Task<Result<LogSource, LogRepositoryError>> * CancellationTokenSource
        | Initialized of LogSource

    type private InnerTaskParams =
        {
            Text: LogSourceText
            FileStorage: FileStorage
            Cts: CancellationTokenSource
            LogSourceId: LogSourceId
            Logger: LogRepositoryLogger
            Observer: IObserver<LogPosition>
        }

    type private State =
        {

            Items:  Map<LogSourceId, LogSourceState>
        }

    type private Msg =
        | ParseText of LogSourceId * LogSourceText * AsyncReplyChannel<LogRepositoryParseTextRequestResult>
        | AppendLogBatch of LogSourceId * LogPosition seq

    let private createMemoryStream (logCreateMemoryStreamError: exn -> unit) (text: LogSourceText) (ct: CancellationToken) =
        try
            (
                new CancellableStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(text.Value)) :> Stream,
                    ct
                ) :> Stream
                , LogStreamSource.MemoryStream
            )
            |> Ok
        with ex ->
            logCreateMemoryStreamError ex
            Error ()

    let private initializationLogSourceItemTask (innerTaskParams: InnerTaskParams) =
        task {
            let logger = innerTaskParams.Logger
            let logSourceId = innerTaskParams.LogSourceId
            do logger.LogInitializingLogSourceItem logSourceId

            let! result = 
                innerTaskParams.FileStorage.CreateTmpFileTask innerTaskParams.Text innerTaskParams.Cts.Token
                |> _.ConfigureAwait(false)

            let streamResult =
                match result with
                | Ok filePath -> 
                    try
                        (
                            new CancellableStream(
                                File.Open(filePath |> FilePath.value, FileMode.Open, FileAccess.ReadWrite),
                                innerTaskParams.Cts.Token
                            ) :> Stream
                            , LogStreamSource.TempFile filePath
                        )
                        |> Ok
                    with ex ->
                        logger.LogOpenTmpFileError logSourceId ex
                        createMemoryStream (logger.LogCreateMemoryStreamError logSourceId) innerTaskParams.Text innerTaskParams.Cts.Token

                | Error err ->
                    logger.LogCreateTmpFileError logSourceId err
                    createMemoryStream (logger.LogCreateMemoryStreamError logSourceId) innerTaskParams.Text innerTaskParams.Cts.Token

            match streamResult with
            | Ok (stream, streamSource) ->
                let logSource : LogSource =
                    {
                        Id = logSourceId
                        Cts = innerTaskParams.Cts
                        Stream = stream
                        StreamSource = streamSource
                        ParseTask =
                            Task.Factory.StartNew(
                                Action (fun () ->
                                    let parseParams = innerTaskParams
                                    let parseResult = TechLogParser.parseStream parseParams.Observer (sprintf "%O" parseParams.LogSourceId) stream
                                    match parseResult with
                                    | Ok () -> parseParams.Logger.LogLogStreamParsedSuccessfully parseParams.LogSourceId
                                    | Error err -> parseParams.Logger.LogLogStreamParsingError parseParams.LogSourceId err
                                ),
                                innerTaskParams.Cts.Token,
                                TaskCreationOptions.PreferFairness ||| TaskCreationOptions.LongRunning,
                                TaskScheduler.Default
                            )
                        Logs = []
                    }

                do logger.LogLogSourceItemInitialized logSourceId

                return
                    logSource |> Ok
            | Error _ ->
                innerTaskParams.Observer.OnError(LogParsingException (LogRepositoryError.LogSourceItemInitializationError logSourceId))
                return LogRepositoryError.LogSourceItemInitializationError logSourceId |> Error
        }

    let private initializingLogSourceItem
        (fileStorage: FileStorage)
        (logger: LogRepositoryLogger)
        (logSourceId: LogSourceId)
        (text: LogSourceText)
        (observer: IObserver<LogPosition>)
        =

        let cts = new CancellationTokenSource()

        let innerTaskParams =
            {
                Text = text
                FileStorage = fileStorage
                Cts = cts;
                LogSourceId = logSourceId
                Logger = logger
                Observer = observer
            }

        let task =
            Task.Factory
                .StartNew(
                    Func<objnull, Task<Result<LogSource, LogRepositoryError>>> (fun state -> initializationLogSourceItemTask (state :?> InnerTaskParams)),
                    innerTaskParams,
                    cts.Token,
                    TaskCreationOptions.PreferFairness,
                    TaskScheduler.Default
                )
                .Unwrap()
                //.ContinueWith()

        LogSourceState.Initializing (task, cts)

    let private handleParseText
        (appSubject: AppSubject)
        (initializingLogSourceItem: LogSourceId -> LogSourceText -> IObserver<LogPosition> -> LogSourceState)
        (logSourceId: LogSourceId)
        (logSourceText: LogSourceText)
        (reply: AsyncReplyChannel<LogRepositoryParseTextRequestResult>)
        (state: State)
        : State
        =
        match state.Items |> Map.tryFind logSourceId with
        | None ->
            let observer = appSubject.GetObserver logSourceId
            let item = initializingLogSourceItem logSourceId logSourceText observer

            reply.Reply(LogRepositoryParseTextRequestResult.Accepted)

            { state with Items = state.Items |> Map.add logSourceId item }

        | Some logSourceItem ->
            match logSourceItem with
            | Initializing _ ->
                reply.Reply(LogRepositoryParseTextRequestResult.PreviousInProgress)
                state

            | Initialized logSource ->
                if not (logSource.ParseTask.IsCanceled || logSource.ParseTask.IsFaulted || logSource.ParseTask.IsCompleted) then
                    reply.Reply(LogRepositoryParseTextRequestResult.PreviousInProgress)
                    state
                else
                    match logSource.StreamSource with
                    | LogStreamSource.MemoryStream
                    | LogStreamSource.TempFile _ ->
                        reply.Reply(LogRepositoryParseTextRequestResult.PreviousNotStorred)
                        state

                    | LogStreamSource.UserFile _ ->
                        // TODO: 
                        logSource.Stream.Dispose()
                        reply.Reply(LogRepositoryParseTextRequestResult.Accepted)
                        state
                        // let newItem = 


    let private agent
        (handleParseText: LogSourceId -> LogSourceText -> AsyncReplyChannel<LogRepositoryParseTextRequestResult> -> State -> State)
        =
        new MailboxProcessor<Msg>(
            (fun processor ->
                let rec running (state: State) =
                    async {
                        let! msg = processor.Receive()

                        // not implemented
                        match msg with
                        | Msg.ParseText (logSourceId, logSourceText, reply) ->
                            return! running (state |> handleParseText logSourceId logSourceText reply)

                        | Msg.AppendLogBatch (logSourceId, logs) ->
                            let newState =
                                match state.Items |> Map.tryFind logSourceId with
                                | Some item -> 
                                    { state with Items = state.Items |> Map.add logSourceId item }
                                | None -> state
                            return! running newState
                    }

                running { Items = Map.empty }
            )
        )

    let init (appSubject: AppSubject) (fileStorage: FileStorage) (logger: LogRepositoryLogger) =
        let agent = agent (handleParseText appSubject (initializingLogSourceItem fileStorage logger))

        let subscription =
            appSubject
            :> IObservable<LogSourceId * LogPosition seq>
            |> Observable.subscribeNext
                (fun (key, l) ->
                    agent.Post(Msg.AppendLogBatch(key, l))
                )
        {
            ParseText = fun (logSourceId: LogSourceId) (logSourceText: LogSourceText) ->
                agent.PostAndReply(fun reply -> Msg.ParseText (logSourceId, logSourceText, reply))
            Dispose = fun () ->
                subscription.Dispose()
                agent.Dispose()
        }
