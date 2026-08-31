namespace LogParser.ElmishApp.Models

open System
open LogParser.App.LogRepository
open LogParser.App

type LogFileListModel =
    {
        LogFileModelList: LogFileModel list
        SelectedLogFileModelId: int
        InitLogRepository: LogSourceId -> LogRepository
    }

module LogFileListModel =

    open System
    open LogParser.ElmishApp.Types

    type Msg =
        | AddNewLogFile
        | SelectLogFileId of int
        | LogFileModelMsg of int * LogFileModel.Msg

    let init (initLogRepository: LogSourceId -> LogRepository) =
        let logSourceId = LogSourceId.create ()
        let logRepository = initLogRepository logSourceId
        let logFileModel = LogFileModel.initNew (1) logRepository
        {
            LogFileModelList =
                [
                    logFileModel
                ]
            SelectedLogFileModelId = logFileModel.Id
            InitLogRepository = initLogRepository
        }

    let addLogFileModel (m: LogFileListModel) =
        let length = m.LogFileModelList.Length
        let id = length + 1
        let logSourceId = LogSourceId.create ()
        let logRepository = m.InitLogRepository logSourceId
        let logFileModel = LogFileModel.initNew (1) logRepository
        { m with
            LogFileModelList = m.LogFileModelList |> List.insertAt length logFileModel
            SelectedLogFileModelId = id
        }

    let selecteLogFileTitle (m: LogFileListModel) =
        m.LogFileModelList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileModelId)
        |> Option.map (fun f -> f |> LogFileModel.title)

    let selecteLogFileName (m: LogFileListModel) =
        m.LogFileModelList
        |> List.tryFind (fun f -> f.Id = m.SelectedLogFileModelId)
        |> Option.map (fun f -> f |> LogFileModel.fullPath)

    let inline withLogFileList logFileList (m: LogFileListModel) =
        { m with LogFileModelList = logFileList }

