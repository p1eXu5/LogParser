namespace LogParser.ElmishApp.Models


type TechLogListModel =
    {
        TechLogList: TechLogModel list
        PinnedFieldName: string option
    }

module TechLogListModel =

    open System
    open LogParser.Types

    type Msg =
        | TechLogModelMsg of Guid * TechLogModel.Msg

    let init logs =
        let uiProps =
            {
                IsExpanded = false
            }
        logs
        |> List.map (fun l -> 
            match l with
            | TechLog.JsonLog l ->
                let techLogModel = TechJsonLogModel.init (l)
                TechLogModel.JsonLogModel (uiProps, techLogModel)
            | TechLog.TextLog l ->
                let (textLogModel, _) = TextLogModel.init (l)
                TechLogModel.TextLogModel (uiProps, textLogModel)
        )
        |> fun models ->
            {
                TechLogList = models
                PinnedFieldName = None
            }

    let inline length (m: TechLogListModel) =
        m.TechLogList.Length

    let inline withTechLogList techLogList (m: TechLogListModel) =
        { m with TechLogList = techLogList }


namespace LogParser.ElmishApp.TechLogListModel

open Elmish
open Elmish.WPF
open p1eXu5.FSharp.ElmishExtensions
open LogParser.ElmishApp
open LogParser.ElmishApp.Models
open LogParser.ElmishApp.Models.TechLogListModel

module Program =

    let update msg model =
        match msg with
        | Msg.TechLogModelMsg (id, smsg) ->
            model
            |> Model.map _.TechLogList withTechLogList
                (List.mapFirst (TechLogModel.logId >> (=) id) (TechLogModel.Program.update smsg))

type IBindings =
    interface
        abstract TechLogList: TechLogModel.IBindings seq
    end

module Bindings =

    let private __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<TechLogListModel, TechLogListModel.Msg> list =
        [
            nameof __.TechLogList
                |> Binding.subModelSeq (
                    (fun m -> m.TechLogList),
                    (fun (m, sm) -> {| TechLogModel = sm; PinnedFieldName = m.PinnedFieldName |}),
                    (_.TechLogModel >> TechLogModel.logId),
                    Msg.TechLogModelMsg,
                    TechLogModel.Bindings.bindings
                )
        ]
