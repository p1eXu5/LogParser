namespace LogParser.DesktopClient.ElmishApp.Models

open System
open LogParser.Core.Types

type TechLogModel =
    {
        Id: Guid
        IsExpanded: bool
        LogLevel: string
        Timestamp: DateTimeOffset option
        Message: string
        HierarchicalTraceId: string // TODO: remove after make log model hierarchy
        ServiceName: string
        TraceId: string // TODO: remove after make log model hierarchy
        Log: TechLog
        Fields: TechFieldModel list
        Children: TechLogModel list
        IsNestedLog: bool // TODO: remove after make log model hierarchy
        HierarchyLevel: int
    }


module TechLogModel=

    open LogParser.Core.Types
    open LogParser.DesktopClient.ElmishApp
    open LogParser.DesktopClient.ElmishApp.Models

    let [<Literal>] DEFAULT_FIELD_VALUE = "___"


    let hierarchyLevel (hierarchicalTraceId: string) =
        hierarchicalTraceId
        |> Seq.fold (fun level ch ->
           if ch = '.' then level + 1
           else level
        ) -1
        |> (fun level ->
            hierarchicalTraceId 
            |> Seq.tryLast 
            |> Option.map (fun ch ->
                if ch = '.' then level
                else level + 1
            )
            |> Option.defaultValue level
        )

    let init (log: TechLog) =
        let (mainFields, otherFields) =
            log.Fields
            |> List.sortBy TechField.orderOrKey
            |> List.map TechFieldModel.init
            |> List.partition (fun f -> 
                match f.TechField with
                | TechField.Timespan _
                | TechField.Level _
                | TechField.HierarchicalTraceId _ // TODO: remove after make log model hierarchy
                | TechField.TraceId _ // TODO: remove after make log model hierarchy
                | TechField.Message _
                | TechField.MessageBoddied _
                | TechField.MessageBoddiedWithPostfix _ -> true
                | _ -> false
            )


        let mainFieldValue key =
            mainFields
            |> List.tryFind (fun f ->
                f.TechField
                |> TechField.orderOrKey
                |> fun fieldKey -> fieldKey.Equals(key, StringComparison.OrdinalIgnoreCase)
            )
            |> Option.bind (fun f -> f.Text)
            

        let otherFieldValue key =
            otherFields
            |> List.tryFind (fun f ->
                f.TechField
                |> TechField.orderOrKey
                |> fun fieldKey -> fieldKey.Equals(key, StringComparison.OrdinalIgnoreCase)
            )
            |> Option.bind (fun f -> f.Text)

        let orDefault = Option.defaultValue DEFAULT_FIELD_VALUE

        let timestamp =
            mainFieldValue "0"
            |> Option.orElseWith (fun _ -> otherFieldValue "Time")
            |> Option.bind (fun v ->
                match DateTimeOffset.TryParse(v) with
                | true, dt -> dt |> Some
                | false, _ -> None
            )

        let level = mainFieldValue "1" |> orDefault
        let message = mainFieldValue "2" |> orDefault
        
        // TODO: remove after make log model hierarchy
        let hierarchicalTraceId = mainFieldValue "6" |> orDefault
        let serviceName = otherFieldValue "ServiceName" |> orDefault
        let traceId = mainFieldValue "5" |> orDefault

        let hierarchyLevel = hierarchyLevel hierarchicalTraceId
        let isNestedLog = hierarchyLevel > 0

        {
            Id = Guid.NewGuid()
            IsExpanded = false
            LogLevel = level
            Timestamp = timestamp
            Message = message
            TraceId = traceId
            HierarchicalTraceId = hierarchicalTraceId
            ServiceName = serviceName
            Log = log
            Fields = log.Source |> Option.map (fun s -> TechFieldModel.init s :: (mainFields @ otherFields)) |> Option.defaultWith (fun () -> (mainFields @ otherFields))
            Children = []
            IsNestedLog = isNestedLog
            HierarchyLevel = hierarchyLevel
        }

    let tryFindField fieldName (techLogModel: TechLogModel) =
        techLogModel.Log
        |> TechLog.tryFindField fieldName