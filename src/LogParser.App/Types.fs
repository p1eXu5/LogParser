namespace LogParser.App

open System
open LogParser.Types

type FilePath = private FilePath of string

type ClientId = private ClientId of Guid

type LogParseMsg =
    | LogPosition of LogPosition
    | LogPositionBunch of LogPosition list
    | ParsingError of string


module FilePath =
    open System.IO

    let create filePath =
        if File.Exists filePath then
            filePath |> FilePath |> Ok
        else
            "File does not exist" |> Error

    let value (FilePath v) = v

module ClientId =
    let create () = Guid.CreateVersion7() |> ClientId
    let value (ClientId v) = v