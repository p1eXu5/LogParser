namespace LogParser.ElmishApp.Models

open LogParser.ElmishApp.Types
open System.IO
open p1eXu5.FSharp.ElmishExtensions
open LogParser.App
open LogParser.App.LogRepository

type LogFileModel =
    {
        Id: int
        Title: string option
        LogFile: LogFile option
        TechLogListModel: TechLogListModel
        ImportLogsState: AsyncDeferredState
    }

module LogFileModel =

    open System

    type Msg = 
        | TechLogListModelMsg of TechLogListModel.Msg
        | PastFromClipboardRequested of AsyncOperation<string, ParseTextRequestResult>

    let initNew (id: int) =
        {
            Id = id
            LogFile = None
            Title = None
            TechLogListModel = TechLogListModel.init []
            ImportLogsState = AsyncDeferredState.NotRequested
        }

    let inline fileNameWithoutExtension (m: LogFileModel) =
        m.LogFile
        |> Option.bind (LogFile.fileNameWithoutExtension)

    let inline fullPath (m: LogFileModel) =
        m.LogFile
        |> Option.bind (LogFile.fullPath)

    let title (m: LogFileModel) =
        match m.Title with
        | Some v -> v
        | None ->
            m
            |> fileNameWithoutExtension
            |> Option.defaultValue "New"

    let inline withTechLogListModel techLogListModel (m: LogFileModel) =
        { m with TechLogListModel = techLogListModel }


