namespace LogParser.App

open System.IO
open System.Threading
open LogParser.App.Abstractions

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
// FileStorageLogger
// --------------------------------------------

type FileStorageLogger =
    {
        LogFailToInitialize: exn -> unit
        LogFailToCreateStream: exn -> unit
    }

// --------------------------------------------
// FileStorage
// --------------------------------------------

type FileStorage =
    {
        State: FileStorageState
        Logger: FileStorageLogger
    }
    interface IStorage with
        member this.GetStream (): Result<CancellableStream,string> = 
            this.State |> FileStorageState.stream this.Logger.LogFailToCreateStream


module FileStorage =

    let init logger =
        try
            Path.GetTempFileName()
            |> FilePath.create
            |> Result.map (fun filePath ->
                {
                    State = filePath |> FileStorageState.Initialized
                    Logger = logger
                }
            )
        with ex ->
            logger.LogFailToInitialize ex
            Error "Could not initialize FileStorage"

// --------------------------------------------
// FileStorageFactory
// --------------------------------------------

module StorageFactory =

    let initFileStorage logger =
        { new IStorageFactory<IStorage> with
            member _.Init () = FileStorage.init logger |> Result.map (fun s -> s :> IStorage)
        }