namespace LogParser.DesktopClient.ElmishApp.Models

open System
type CoreTechnoLog = LogParser.Core.Types.TechnoLog

type TechnoLog =
    {
        Id: Guid
        IsExpanded: bool
        LogLevel: string
        Timestamp: string
        Message: string
        HierarchicalTraceId: string // TODO: remove after make log model hierarchy
        Log: CoreTechnoLog
        Fields: TechnoField list
        Children: TechnoLog list
        IsNestedLog: bool // TODO: remove after make log model hierarchy
        HierarchyLevel: int
    }


module TechnoLog=

    open LogParser.Core.Types
    open LogParser.DesktopClient.ElmishApp


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

    let init (log: TechnoLog) =
        let (mainFields, otherFields) =
            log.Fields
            |> List.sortBy TechnoFields.order
            |> List.map TechnoField.init
            |> List.partition (fun f -> 
                match f.TechnoField with
                | TechnoField.Timespan _
                | TechnoField.Level _
                | TechnoField.HierarchicalTraceId _ // TODO: remove after make log model hierarchy
                | TechnoField.Message _
                | TechnoField.MessageBoddied _
                | TechnoField.MessageParameterized _ -> true
                | _ -> false
            )


        let valueOf k =
            mainFields |> List.tryFind (fun f -> f.TechnoField |> TechnoFields.order |> (=) k) |> Option.bind (fun f -> f.Text) |> Option.defaultValue "___"

        let timestamp = valueOf "0"
        let level = valueOf "1"
        let message = valueOf "2"
        
        // TODO: remove after make log model hierarchy
        let hierarchicalTraceId = valueOf "6"
        let hierarchyLevel = hierarchyLevel hierarchicalTraceId
        let isNestedLog = hierarchyLevel > 0

        {
            Id = Guid.NewGuid()
            IsExpanded = false
            LogLevel = level
            Timestamp = timestamp
            Message = message
            HierarchicalTraceId = hierarchicalTraceId
            Log = log
            Fields = log.Source |> Option.map (fun s -> TechnoField.init s :: (mainFields @ otherFields)) |> Option.defaultWith (fun () -> (mainFields @ otherFields))
            Children = []
            IsNestedLog = isNestedLog
            HierarchyLevel = hierarchyLevel
        }
