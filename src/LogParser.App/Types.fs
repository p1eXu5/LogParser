namespace LogParser.App

open System
open LogParser.Types

/// Log source identifier. Used in AppSubject.
[<Struct>]
type LogSourceId = private LogSourceId of Guid

[<Struct>]
type TechLogId =
    private {
        Ind: int
        Length: int
        StartIndex: int64
        EndIndex: int64
    }

type TechLogMap = Map<TechLogId, TechLog>

type FilePath = private FilePath of string
    //private
    //| TmpFilePath of string
    //| UserFilePath of string

/// Text containing logs.
type LogSourceText = private LogSourceText of string

type LogParseMsg =
    | LogPosition of TechLogPosition
    | LogPositionBunch of TechLogPosition list
    | ParsingError of string


type LogFile =
    | MemoryStream
    | TempFile of FilePath
    | UserFile of FilePath

type FieldKey = string

// ===========================
// Modules
// ===========================

module FilePath =
    open System.IO

    let create filePath =
        if File.Exists filePath then
            filePath |> FilePath |> Ok
        else
            "Temp file does not exist" |> Error

    let createUnsafe filePath =
        match create filePath with
        | Ok p -> p
        | Error err ->
            raise (InvalidOperationException(err))

    let value (FilePath filePath) = filePath

module LogSourceId =
    let create () = Guid.CreateVersion7() |> LogSourceId
    let value (LogSourceId v) = v

/// Text containing logs.
module LogSourceText =
    let create (text: string) =
        if String.IsNullOrWhiteSpace(text) then
            Error "text must contains information"
        else
            text |> LogSourceText |> Ok

    let createUnsafe (text: string) =
        match create text with
        | Ok l -> l
        | Error err ->
            raise (InvalidOperationException(err))

    let value (LogSourceText v) = v

type LogSourceText with
    member this.Value with get () = this |> LogSourceText.value

module LogFile =
    let isNotUserFile (logFile: LogFile) =
        match logFile with
        | LogFile.UserFile _ -> false
        | _ -> true

module TechLogId =
    let fromTechLogPosition (ind: int) (length: int) (techLogPosition: TechLogPosition) =
        {
            Ind = ind
            Length = length
            StartIndex = techLogPosition.Start.Index
            EndIndex = techLogPosition.End.Index
        }
