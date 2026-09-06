namespace LogParser.ElmishApp.TechLogModel

open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TechLogModel
open LogParser.App
open LogParser.Types
open LogParser.App.LogRepository
open Microsoft.Extensions.Logging

type IBindings =
    interface
        abstract Log: obj with get
        abstract IsTechLog: bool with get
        abstract LogLevel: string option with get
        abstract Timestamp: string option with get
        abstract Message: string option with get
        abstract Fields: TechFieldModel.IBindings seq with get
    end

module Bindings =

    open Elmish.WPF

    let private __ = Unchecked.defaultof<IBindings>

    let fieldValue fieldKey (_, indexedTechLod) =
        match indexedTechLod.TechLog with
        | TechLog.JsonLog l ->
            indexedTechLod
            |> _.Fields
            |> Option.bind (fun map ->
                match map |> Map.tryFind fieldKey with
                | Some ind -> l.Fields |> List.item ind |> TechJsonLogField.value |> Some
                | None -> None
            )
        | _ -> None

    let message (_, indexedTechLod) =
        match indexedTechLod.TechLog with
        | TechLog.JsonLog l ->
            indexedTechLod
            |> _.Fields
            |> Option.bind (fun map ->
                match map |> Map.tryFind TechJsonLogField.Keys.MESSAGE with
                | Some ind -> l.Fields |> List.item ind |> TechJsonLogField.value |> Some
                | None -> None
            )
        | TechLog.TextLog tlog -> tlog |> Some

    let bindings () : Binding<TechLogModel * IndexedTechLog, TechLogModel.Msg> list =
        [
            nameof __.IsTechLog
                |> Binding.oneWay (snd >> _.IsTechJson)

            nameof __.LogLevel
                |> Binding.oneWayOpt (fieldValue TechJsonLogField.Keys.LEVEL)

            nameof __.Timestamp
                |> Binding.oneWayOpt (fieldValue TechJsonLogField.Keys.TIMESTAMP)

            nameof __.Message
                |> Binding.oneWayOpt (message)

            nameof __.Fields
                |> Binding.subModelSeq (
                    (fun m ->
                        match (snd m).TechLog with
                        | TechLog.TextLog _ -> []
                        | TechLog.JsonLog t -> t.Fields),
                    (fun (_, f: TechJsonLogField) -> f |> TechFieldModel.init),
                    (fun f -> f.Key),
                    (Msg.TechFieldMsg),
                    TechFieldModel.Bindings.bindings
                )
            (*

            "CopyCommand"
                |> Binding.cmd Msg.CopyLogCommand
                )
            "Log"
                |> Binding.oneWayOpt (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    match l.TechLogModel with
                    | TechLogModel.JsonLogModel (_, tl) ->
                        tl.Fields
                        |> List.map (fun f -> f.TechField)
                        |> LogParser.Types.TechJsonLogField.toString 1
                        |> Some
                    | TechLogModel.TextLogModel (_, t) ->
                        t.Log |> Some
                )

            "PinnedValue"
                |> Binding.oneWayOpt (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    l.PinnedFieldName
                    |> Option.bind (fun fn ->
                        match l.TechLogModel with
                        | TechLogModel.JsonLogModel (_, tl) -> 
                            tl.Fields
                            |> List.tryFind(fun f -> f.Key = fn)
                            |> Option.bind (fun f -> f.Text )
                        | TechLogModel.TextLogModel _ -> None
                    )
                ) 

            "HierarchyLevel"
                |> Binding.oneWay (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    match l.TechLogModel with
                    | TechLogModel.JsonLogModel (_, tl) -> tl.HierarchyLevel
                    | TechLogModel.TextLogModel _ -> 0
                )

            *)
        ]