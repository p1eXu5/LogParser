namespace LogParser.ElmishApp.Models

// TODO: rename to TechLogListFilter
type TechLogListModel =
    {
        TechLogModelList: TechLogModel list
        PinnedFieldNameA: string option
        PinnedFieldNameB: string option
    }

module TechLogListModel =

    open System
    open LogParser.Types

    type Msg =
        | TechLogModelMsg of int * TechLogModel.Msg

    let init logs =
        //let uiProps =
        //    {
        //        IsExpanded = false
        //    }
        //logs
        //|> List.map (fun l -> 
        //    match l with
        //    | TechLog.JsonLog l ->
        //        let techLogModel = JsonLogModelV2.init (l)
        //        TechLogModel.JsonLogModel (uiProps, techLogModel)
        //    | TechLog.TextLog l ->
        //        let (textLogModel, _) = TextLogModelV2.init (l)
        //        TechLogModel.TextLogModel (uiProps, textLogModel)
        //)
        //|> fun models ->
        {
            TechLogModelList = []
            PinnedFieldNameA = None
            PinnedFieldNameB = None
        }

    let inline length (m: TechLogListModel) =
        m.TechLogModelList.Length

    let inline withTechLogList techLogList (m: TechLogListModel) =
        { m with TechLogModelList = techLogList }


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
            //|> Model.map _.TechLogModelList withTechLogList
            //    (List.mapFirst (TechLogModel.logId >> (=) id) (TechLogModel.Program.update smsg))

type IBindings =
    interface
        abstract TechLogList: TechLogModel.IBindings seq
    end

module Bindings =

    let private __ = Unchecked.defaultof<IBindings>

    let bindings () : Binding<TechLogListModel, TechLogListModel.Msg> list =
        [
            //nameof __.TechLogList
            //    |> Binding.subModelSeq (
            //        (fun m -> m.TechLogModelList),
            //        (fun (m, sm) -> {| TechLogModel = sm; PinnedFieldName = m.PinnedFieldNameA |}),
            //        (_.TechLogModel >> TechLogModel.logId),
            //        Msg.TechLogModelMsg,
            //        TechLogModel.Bindings.bindings
            //    )
        ]
