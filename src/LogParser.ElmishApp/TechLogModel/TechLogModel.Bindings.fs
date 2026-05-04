namespace LogParser.ElmishApp.TechLogModel

open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TechLogModel

type IBindings =
    interface
        abstract Log: obj with get
    end

module Bindings =

    open Elmish.WPF

    let private __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<{| TechLogModel: TechLogModel; PinnedFieldName: string option |}, TechLogModel.Msg> list =
        [
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

            "IsTechLog"
                |> Binding.oneWay (fun l ->
                    l.TechLogModel
                    |> function TechLogModel.JsonLogModel _ -> true | _ -> false
                )

            "LogLevel"
                |> Binding.oneWayOpt (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    match l.TechLogModel with
                    | TechLogModel.JsonLogModel (_, tl) -> tl.LogLevel |> Some
                    | TechLogModel.TextLogModel _ -> None
                )

            "Timestamp"
                |> Binding.oneWayOpt (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    match l.TechLogModel with
                    | TechLogModel.JsonLogModel (_, tl) -> tl.Timestamp
                    | TechLogModel.TextLogModel _ -> None
                )

            "Message"
                |> Binding.oneWayOpt (fun (l: {| TechLogModel: TechLogModel; PinnedFieldName: string option |}) ->
                    match l.TechLogModel with
                    | TechLogModel.JsonLogModel (_, tl) -> tl.Message |> Some
                    | TechLogModel.TextLogModel _ -> None
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

            (*
            "CopyCommand"
                |> Binding.cmd Msg.CopyLogCommand

            "Fields"
                |> Binding.subModelSeq (
                    (fun l ->
                            match l.TechLogModel with
                            | TechLogModel.TextLogModel _ -> []
                            | TechLogModel.JsonLogModel t -> t.Fields),
                    (fun (_, f: TechFieldModel) -> f),
                    (fun f -> f.Key),
                    (Msg.TechFieldMsg),
                    TechFieldModel.Bindings.bindings
                )
            *)
        ]