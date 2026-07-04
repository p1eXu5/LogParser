namespace LogParser.App

open System
open LogParser.Types

type FilePath =
    private
    | TmpFilePath of string
    | UserFilePath of string

[<Struct>]
type ClientId = private ClientId of Guid

[<Struct>]
type LogSourceId = private LogSourceId of Guid

type LogSourceText = private LogSourceText of string

type LogParseMsg =
    | LogPosition of LogPosition
    | LogPositionBunch of LogPosition list
    | ParsingError of string


type internal LogStreamSource =
    | MemoryStream
    | TempFile of FilePath
    | UserFile of FilePath

module FilePath =
    open System.IO

    let createTmp filePath =
        if File.Exists filePath then
            filePath |> FilePath.TmpFilePath |> Ok
        else
            "Temp file does not exist" |> Error

    let createTmpUnsafe filePath =
        match createTmp filePath with
        | Ok p -> p
        | Error err ->
            raise (InvalidOperationException(err))

    let createUser filePath =
        if File.Exists filePath then
            filePath |> FilePath.UserFilePath |> Ok
        else
            "User file does not exist" |> Error

    let value = function
        | FilePath.TmpFilePath v
        | FilePath.UserFilePath v -> v

module ClientId =
    let create () = Guid.CreateVersion7() |> ClientId
    let value (ClientId v) = v

module LogSourceId =
    let create () = Guid.CreateVersion7() |> LogSourceId
    let value (LogSourceId v) = v

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
