module LogParser.ElmishApp.Loggers.AppSubjectLogger

open Microsoft.Extensions.Logging
open LogParser.App

[<RequireQualifiedAccess>]
module private AppSubjectEvent =
    let RequestingObserver         = EventId(1,  "Requesting Observer")
    let ObserverObtained           = EventId(2,  "Observer Obtained")
    let InnerObserverExists        = EventId(3,  "Inner Observer Exists")
    let InnerObserverCreated       = EventId(4,  "Inner Observer Created")
    let ObserverNext               = EventId(5,  "Observer Next")
    let ObserverError              = EventId(6,  "Observer Error")
    let ObserverCompleted          = EventId(7,  "Observer Completed")
    let SwitchingToIteration       = EventId(8,  "Switching To Iteration")
    let SourceIsDisposing          = EventId(9,  "Source Is Disposing")
    let SourceIsDisposed           = EventId(10, "Source Is Disposed")
    let SourceIsDisposedWithMerged = EventId(11, "Source Is Disposed With Merged")

/// The delegates are created once (module initialization) and reused for every call,
/// so no boxing of the message arguments and no format-string parsing per log entry.
module private AppSubjectMessage =

    let requestingObserver =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.RequestingObserver,
            "Requesting observer for the log source {LogSourceId}")

    let observerObtained =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.ObserverObtained,
            "Observer is obtained for the log source {LogSourceId}")

    let innerObserverExists =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.InnerObserverExists,
            "Inner observer exists for the log source {LogSourceId}")

    let innerObserverCreated =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.InnerObserverCreated,
            "Inner observer has been created for the log source {LogSourceId}")

    let observerNext =
        LoggerMessage.Define<LogSourceId, obj>(
            LogLevel.Trace, AppSubjectEvent.ObserverNext,
            "Next for the log source {LogSourceId}: {Log}")

    let observerError =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Error, AppSubjectEvent.ObserverError,
            "Error in the log source {LogSourceId}")

    let observerCompleted =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.ObserverCompleted,
            "Completed: {LogSourceId}")

    let switchingToIteration =
        LoggerMessage.Define<int>(
            LogLevel.Debug, AppSubjectEvent.SwitchingToIteration,
            "Switching to the {Iteration} iteration")

    let sourceIsDisposing =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.SourceIsDisposing,
            "Disposing the log source {LogSourceId}")

    let sourceIsDisposed =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.SourceIsDisposed,
            "Disposed the log source {LogSourceId}")

    let sourceIsDisposedWithMerged =
        LoggerMessage.Define<LogSourceId>(
            LogLevel.Debug, AppSubjectEvent.SourceIsDisposedWithMerged,
            "Disposed with merged the log source {LogSourceId}")

let init (logger: ILogger<AppSubject>) : AppSubjectLogger =
    {
        LogRequestingObserver =
            fun id -> AppSubjectMessage.requestingObserver.Invoke(logger, id, null)

        LogObserverObtained =
            fun id -> AppSubjectMessage.observerObtained.Invoke(logger, id, null)

        LogInnerObserverExists =
            fun id -> AppSubjectMessage.innerObserverExists.Invoke(logger, id, null)

        LogInnerObserverCreated =
            fun id -> AppSubjectMessage.innerObserverCreated.Invoke(logger, id, null)

        LogObserverNext =
            fun id l -> AppSubjectMessage.observerNext.Invoke(logger, id, box l.Log, null)

        LogObserverError =
            fun id ex -> AppSubjectMessage.observerError.Invoke(logger, id, ex)

        LogObserverCompleted =
            fun id -> AppSubjectMessage.observerCompleted.Invoke(logger, id, null)

        LogSwitchingToIteration =
            fun iteration -> AppSubjectMessage.switchingToIteration.Invoke(logger, iteration, null)

        LogSourceIsDisposing =
            fun id -> AppSubjectMessage.sourceIsDisposing.Invoke(logger, id, null)

        LogSourceIsDisposed =
            fun id -> AppSubjectMessage.sourceIsDisposed.Invoke(logger, id, null)

        LogSourceIsDisposedWithMerged =
            fun id -> AppSubjectMessage.sourceIsDisposedWithMerged.Invoke(logger, id, null)
    }