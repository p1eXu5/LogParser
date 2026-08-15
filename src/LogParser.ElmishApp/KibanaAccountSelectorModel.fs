namespace LogParser.ElmishApp.Models

open System
open p1eXu5.FSharp.ElmishExtensions
open LogParser.Kibana
open LogParser.ElmishApp.Helpers
open LogParser.ElmishApp.KibanaSearchModel.Services

type KibanaAccountSelectorModel =
    {
        KibanaAccounts: KibanaAccountModel list
        SelectedKibanaAccountIndex: int option
    }

module KibanaAccountSelectorModel =

    type Msg =
        | SetSelectedKibanaAccountIndex of int option


namespace LogParser.ElmishApp.KibanaAccountSelectorModel

open System

module Program =

    open System.Windows
    open Microsoft.Extensions.Logging
    open Elmish
    open p1eXu5.FSharp.ElmishExtensions
    open LogParser
    open LogParser.ElmishApp.Models
    open LogParser.ElmishApp.Models.KibanaAccountSelectorModel
    open LogParser.ElmishApp.Interfaces

    //let update (settingsManager: ISettingsManager) (logger: ILogger) msg model =
    //    match msg with
    //    | SetSelectedKibanaAccountIndex ind ->
    //        model |> withSelectedKibanaAccountIndex ind |> setKibanaParams
    //        , Cmd.ofMsg Msg.SaveSelectedKibanaAccount