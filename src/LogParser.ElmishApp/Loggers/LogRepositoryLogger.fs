module LogParser.ElmishApp.Loggers.LogRepositoryLogger

open System
open Microsoft.Extensions.Logging
open LogParser.App
open LogParser.App.LogRepository

[<RequireQualifiedAccess>]
module private LogRepositoryEvent =
    let InitializingLogSourceItem   = EventId(101, "Initializing Log Source Item")
    let LogSourceItemInitialized    = EventId(102, "Log Source Item Initialized")
    let CreateTmpFileError          = EventId(103, "Create Temp File Error")
    let OpenTmpFileError            = EventId(104, "Open Temp File Error")
    let CreateMemoryStreamError     = EventId(105, "Create Memory Stream Error")
    let LogStreamParsedSuccessfully = EventId(106, "Log Stream Parsed Successfully")
    let LogStreamParsingError       = EventId(107, "Log Stream Parsing Error")
    let LogStreamError              = EventId(108, "Log Stream Error")
    let ProcessingMsg               = EventId(109, "Processing Message")
    let UnprocessedMsg              = EventId(110, "Unprocessed Message")

/// Delegates are built once and reused; the template is never re-parsed
/// and the level check happens inside the delegate.
module private LogRepositoryMessage =

    let initializingLogSourceItem =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, LogRepositoryEvent.InitializingLogSourceItem,
            "Initializing the log source item {LogSourceId}")

    let logSourceItemInitialized =
        LoggerMessage.Define<LogSourceId, LogSource>(
            LogLevel.Debug, LogRepositoryEvent.LogSourceItemInitialized,
            "The log source item {LogSourceId} has been initialized from the log file {LogFile}")

    // FileStorageError is a domain value, not an exception, so it goes into the
    // template rather than the exception slot.
    let createTmpFileError =
        LoggerMessage.Define<LogSourceId, FileStorageError>(
            LogLevel.Warning, LogRepositoryEvent.CreateTmpFileError,
            "Failed to create a temp file for {LogSourceId}: {Error}. Falling back to a memory stream")

    let openTmpFileError =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Warning, LogRepositoryEvent.OpenTmpFileError,
            "Failed to open a temp file for {LogSourceId}. Falling back to a memory stream")

    let createMemoryStreamError =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Error, LogRepositoryEvent.CreateMemoryStreamError,
            "Failed to create a memory stream for {LogSourceId}")

    let logStreamParsedSuccessfully =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, LogRepositoryEvent.LogStreamParsedSuccessfully,
            "The log stream {LogSourceId} has been parsed successfully")

    let logStreamParsingError =
        LoggerMessage.Define<LogSourceId, string>(
            LogLevel.Warning, LogRepositoryEvent.LogStreamParsingError,
            "The log stream {LogSourceId} has not been parsed: {Reason}")

    let logStreamError =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Error, LogRepositoryEvent.LogStreamError,
            "The log stream {LogSourceId} has not been parsed")

    let processingMsg =
        LoggerMessage.Define<string, string>(
            LogLevel.Trace, LogRepositoryEvent.ProcessingMsg,
            "The message {Message} is processing. State - {State}")

    let unprocessedMsg =
        LoggerMessage.Define<string, string>(
            LogLevel.Trace, LogRepositoryEvent.UnprocessedMsg,
            "The message {Message} is skipped. State - {State}")

let init (logger: ILogger<LogRepository>) : LogRepositoryLogger =
    {
        LogInitializingLogSourceItem =
            fun id -> LogRepositoryMessage.initializingLogSourceItem.Invoke(logger, id, null)

        LogLogSourceItemInitialized =
            fun id file -> LogRepositoryMessage.logSourceItemInitialized.Invoke(logger, id, file, null)

        LogCreateTmpFileError =
            fun id error -> LogRepositoryMessage.createTmpFileError.Invoke(logger, id, error, null)

        LogOpenTmpFileError =
            fun id ex -> LogRepositoryMessage.openTmpFileError.Invoke(logger, id, ex)

        LogCreateMemoryStreamError =
            fun id ex -> LogRepositoryMessage.createMemoryStreamError.Invoke(logger, id, ex)

        LogLogStreamParsedSuccessfully =
            fun id -> LogRepositoryMessage.logStreamParsedSuccessfully.Invoke(logger, id, null)

        LogLogStreamParsingError =
            fun id reason -> LogRepositoryMessage.logStreamParsingError.Invoke(logger, id, reason, null)

        LogLogStreamError =
            fun id ex -> LogRepositoryMessage.logStreamError.Invoke(logger, id, ex)

        LogProcessingMsg =
            fun msg state -> LogRepositoryMessage.processingMsg.Invoke(logger, msg, state, null)

        LogUnprocessedMsg =
            fun msg state -> LogRepositoryMessage.unprocessedMsg.Invoke(logger, msg, state, null)
    }