namespace LogParser.App

open System
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks
open LogParser.App.Abstractions
open System.Buffers

// --------------------------------------------
// FileStorageState
// --------------------------------------------

type FileStorageState =
    private
    | Initialized of FilePath

module FileStorageState =

    let stream logErrorf (fileStorageState: FileStorageState) =
        match fileStorageState with
        | FileStorageState.Initialized filePath ->
            try
                let fileStream = File.OpenRead(filePath |> FilePath.value)
                new CancellableStream(fileStream) |> Ok
            with ex ->
                logErrorf ex
                Error "Could not create file stream"

// --------------------------------------------
// FileStorage
// --------------------------------------------

type FileStorage =
    {
        CreateTmpFileTask: LogSourceText -> CancellationToken -> Task<Result<FilePath, FileStorageError>>
    }
and
    FileStorageError =
        | TmpFileCreatingError
and
    FileStorageLogger =
        {
            LogFailToInitialize: exn -> unit
            LogFailToCreateStream: exn -> unit
        }


module FileStorage =

    let createTmpFile logger (logSourceText: LogSourceText) (ct: CancellationToken) =
        task {
            try
                let path = Path.GetTempFileName()
                use sw = File.OpenWrite(path)

                let unvalidatedLogs = logSourceText.Value
                let bytesCount = Encoding.UTF8.GetByteCount(unvalidatedLogs)
                let buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(unvalidatedLogs))

                try
                    let _ = Encoding.UTF8.GetBytes(unvalidatedLogs, buffer)
                    do! sw.WriteAsync(buffer, 0, bytesCount, ct).ConfigureAwait(false)
                finally
                    ArrayPool<byte>.Shared.Return(buffer)

                return path |> FilePath.createUnsafe |> Ok
            with ex ->
                logger.LogFailToInitialize ex
                return Error TmpFileCreatingError
        }

    let init (logger: FileStorageLogger) =
        {
            CreateTmpFileTask = createTmpFile logger
        }
