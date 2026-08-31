module LogParser.ElmishApp.Loggers.FileStorageLogger

open System
open Microsoft.Extensions.Logging
open LogParser.App

[<RequireQualifiedAccess>]
module private FileStorageEvent =
    let FailToInitialize   = EventId(51, "FailToInitialize")
    let FailToCreateStream = EventId(52, "FailToCreateStream")

/// Delegates are built once and reused; the template is never re-parsed
/// and the level check happens inside the delegate.
module private FileStorageMessage =

    // The non-generic Define returns Action<ILogger, exn> — no message arguments,
    // just the exception, which is exactly what these two cases need.
    let failToInitialize : Action<ILogger, exn> =
        LoggerMessage.Define(
            LogLevel.Critical, FileStorageEvent.FailToInitialize,
            "Failed to initialize the file storage")

    let failToCreateStream : Action<ILogger, exn> =
        LoggerMessage.Define(
            LogLevel.Error, FileStorageEvent.FailToCreateStream,
            "Failed to create the file stream")

let init (logger: ILogger<FileStorage>) : FileStorageLogger =
    {
        LogFailToInitialize =
            fun ex -> FileStorageMessage.failToInitialize.Invoke(logger, ex)

        LogFailToCreateStream =
            fun ex -> FileStorageMessage.failToCreateStream.Invoke(logger, ex)
    }