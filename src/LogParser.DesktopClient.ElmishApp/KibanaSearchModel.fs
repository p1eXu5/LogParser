namespace LogParser.DesktopClient.ElmishApp.Models

open System
open Elmish.Extensions
open LogParser.Core.Kibana
open LogParser.DesktopClient.ElmishApp.Helpers
open LogParser.DesktopClient.ElmishApp.KibanaSearchModel.Services

type KibanaAccount =
    {
        Name: string
        BaseUri: string option
        Login: string option
        Password: string option
        IndexPattern: string
        IndexFormat: int<year> -> int<month> -> int<day> -> string
        TraceIdQuery: TraceIdQueryType
        SearchSource: string
    }
    with
        override this.ToString() =
            this.BaseUri |> Option.defaultValue this.Name

type KibanaSearchModel =
    {
        KibanaAccounts: KibanaAccount list
        SelectedKibanaAccountIndex: int option
        TraceId: string option
        ServiceNames: string list
        SelectedServiceName: string option
        LogsDate: DateTime option
        LogsStartTime: DateTime option
        LogsEndTime: DateTime option
        LogCountFrom: int option
        LogCountTo: int option

        SearchKibanaLogsParams: SearchKibanaLogsParams option
    }

module KibanaAccount =
    

    let toOption str =
        if String.IsNullOrWhiteSpace(str) then None
        else str |> Some

    let mccore =
        {
            Name = "Mccore"
            BaseUri = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MCCORE_URL", EnvironmentVariableTarget.User) |> toOption
            Login = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MCCORE_LOGIN", EnvironmentVariableTarget.User) |> toOption
            Password = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MCCORE_PASSWORD", EnvironmentVariableTarget.User) |> toOption
            IndexPattern = "filebeat-*"
            IndexFormat = (fun y m d -> sprintf "filebeat-%i.%s.%s" (int y) ((int m).ToString("00")) ((int d).ToString("00")))
            TraceIdQuery = TraceIdQueryType.Indexed
            SearchSource =
                """,
  "_source": [
    "@timestamp",
    "level",
    "fullMessage",
    "TraceId",
    "servicename",
    "container"
  ]
}
                """
        }

    let mir =
        {
            Name = "Mir"
            BaseUri = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MIR_URL", EnvironmentVariableTarget.User) |> toOption
            Login = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MIR_LOGIN", EnvironmentVariableTarget.User) |> toOption
            Password = Environment.GetEnvironmentVariable("LOGPARSER_KIBANA_MIR_PASSWORD", EnvironmentVariableTarget.User) |> toOption
            IndexPattern = "tekno-*"
            IndexFormat = (fun y m d -> sprintf "tekno-%s-%i.%s.%s" (System.Net.WebUtility.UrlEncode("%{[app]}")) (int y) ((int m).ToString("00")) ((int d).ToString("00")))
            TraceIdQuery = TraceIdQueryType.JsonParsed
            SearchSource =
                """,
  "_source": [
    "fullMessage",
    "servicename",
    "container"
  ]
}
                """
        }

module KibanaSearchModel =

    open System
    open LogParser.DesktopClient.ElmishApp.Interfaces

    type Msg =
        | SetSelectedKibanaAccountIndex of int option
        | SetKibanaBaseUri of string option
        | SetKibanaLogin of string option
        | SetKibanaPassword of string option
        | SetTraceId of string option
        | SetLogsDate of DateTime option
        | SetLogsStartTime of DateTime option
        | SetLogsEndTime of DateTime option
        | SetSelectedServiceName of string option
        | SetLogCount of string option
        | SetLogCountFrom of string option
        | SaveKibanaBaseUri
        | SaveKibanaLogin
        | SaveSelectedKibanaAccount
        | CopyKibanaRequestToClipboard
        | SearchKibanaLogs of Operation<unit, string seq>
        | PreviousPage
        | NextPage
        | OnError of exn

    [<RequireQualifiedAccess>]
    type Intent =
        | NoIntent
        | FilterOff
        | LoadingOn
        | ProcessLoadedLogs of string option
        | OnError of exn

    let init (settingsManager: ISettingsManager) =
        let getSaved setting = 
            match (settingsManager.Load setting) with
            | :? string as s when not (String.IsNullOrWhiteSpace(s)) -> s |> Some
            | _ -> None

        let getSavedInt setting = 
            match (settingsManager.Load setting) with
            | :? int as v -> v |> Some
            | _ -> None

        let kibanaMccoreBaseUrl = getSaved "KibanaMccoreBaseUri"
        let kibanaMccoreLogin = getSaved "KibanaMccoreLogin"

        let kibanaMirBaseUrl = getSaved "KibanaMirBaseUri"
        let kibanaMirLogin = getSaved "KibanaMirLogin"

        let selectedKibanaAccount = getSavedInt "SelectedKibanaAccount" |> Option.orElse (0 |> Some)

        let withStored baseUrl login account =
            match baseUrl, login with
            | Some u, Some l -> { account with BaseUri = kibanaMccoreBaseUrl; Login = kibanaMccoreLogin }
            | None, Some l -> { account with Login = kibanaMccoreLogin }
            | Some u, None -> { account with BaseUri = kibanaMccoreBaseUrl }
            | _ -> account

        let kibanaAccounts =
            [
                KibanaAccount.mccore |> withStored kibanaMccoreBaseUrl kibanaMccoreLogin
                KibanaAccount.mir |> withStored kibanaMirBaseUrl kibanaMirLogin
            ]

        {
            KibanaAccounts = kibanaAccounts
            SelectedKibanaAccountIndex = selectedKibanaAccount
            TraceId = None
            ServiceNames = services
            SelectedServiceName = None
            LogCountFrom = None
            LogCountTo = 500 |> Some
            LogsDate = None
            LogsStartTime = None
            LogsEndTime = None
            SearchKibanaLogsParams = None
        }

    // ---------------------------- accessors

    let withSelectedKibanaAccountIndex ind (m: KibanaSearchModel) = { m with SelectedKibanaAccountIndex = ind }
    let selectedKibanaAccount (m: KibanaSearchModel) =
        m.SelectedKibanaAccountIndex
        |> Option.map (fun ind -> m.KibanaAccounts |> List.item ind)

    let selectedKibanaAccountName (m: KibanaSearchModel) =
        m |> selectedKibanaAccount |> Option.map (fun acc -> acc.Name)

    let selectedKibanaBaseUri (m: KibanaSearchModel) =
        m |> selectedKibanaAccount |> Option.bind (fun acc -> acc.BaseUri)

    let setKibanaBaseUri kibanaBaseUri (m: KibanaSearchModel) =
        match m |> selectedKibanaAccount with
        | Some acc ->
            let ind = m.SelectedKibanaAccountIndex.Value
            let acc = { acc with BaseUri = kibanaBaseUri }
            { m with
                KibanaAccounts =
                    m.KibanaAccounts
                    |> List.removeAt ind
                    |> List.insertAt ind acc
            }
        | None -> m

    let selectedKibanaLogin (m: KibanaSearchModel) =
        m |> selectedKibanaAccount |> Option.bind (fun acc -> acc.Login)

    let setKibanaLogin login (m: KibanaSearchModel) =
        match m |> selectedKibanaAccount with
        | Some acc ->
            let ind = m.SelectedKibanaAccountIndex.Value
            let acc = { acc with Login = login }
            { m with
                KibanaAccounts =
                    m.KibanaAccounts
                    |> List.removeAt ind
                    |> List.insertAt ind acc
            }
        | None -> m

    let selectedKibanaPassword (m: KibanaSearchModel) =
        m |> selectedKibanaAccount |> Option.bind (fun acc -> acc.Password)

    let setKibanaPassword pass (m: KibanaSearchModel) =
        match m |> selectedKibanaAccount with
        | Some acc ->
            let ind = m.SelectedKibanaAccountIndex.Value
            let acc = { acc with Password = pass }
            { m with
                KibanaAccounts =
                    m.KibanaAccounts
                    |> List.removeAt ind
                    |> List.insertAt ind acc
            }
        | None -> m

    let getServiceNames m = m.ServiceNames
    let setSelectedServiceNames serviceName (m: KibanaSearchModel) =
        match serviceName with
        | Some sn when sn |> notEmpty ->
            { m with SelectedServiceName = serviceName }
        | _ -> { m with SelectedServiceName = None }

    let getLogCountFrom m = m.LogCountFrom |> Option.map (sprintf "%i")
    let setLogCountFrom (logCountFrom: string option) m =
        match logCountFrom with
        | Some lc ->
            match Int32.TryParse(lc) with
            | true, v when v > 0 -> { m with LogCountFrom = v |> Some }
            | _ -> { m with LogCountFrom = None }
        | None -> { m with LogCountFrom = None }

    let getLogCount m = m.LogCountTo |> Option.map (sprintf "%i")
    let setLogCount (logCount: string option) m =
        match logCount with
        | Some lc ->
            match Int32.TryParse(lc) with
            | true, v when v > 0 -> { m with LogCountTo = v |> Some }
            | _ -> { m with LogCountTo = None }
        | None -> { m with LogCountTo = None }


    let toSearchKibanaLogsParams (m: KibanaSearchModel) =
        match m |> selectedKibanaAccount with
        | Some kibanaAccount ->
            match kibanaAccount.BaseUri, kibanaAccount.Login, kibanaAccount.Password with
            | Some u, Some l, Some p when u |> notEmpty && l |> notEmpty && p |> notEmpty ->
                let tm =
                    match m.TraceId with
                    | Some v ->
                        if v.StartsWith("m:") then
                            v.Substring(2).Trim() |> TraceIdMessage.Message
                        else
                            v |> TraceIdMessage.TraceId
                        |> Some
                    | _ -> None
                {
                    KibanaBaseUri = u
                    Login = l
                    Password = p
                    IndexDate = m.LogsDate
                    StartTime = m.LogsStartTime
                    EndTime = m.LogsEndTime
                    TraceIdMessage = tm
                    From = m.LogCountFrom
                    Size = m.LogCountTo
                    ServiceName = m.SelectedServiceName
                    IndexPattern = kibanaAccount.IndexPattern
                    IndexFormat = kibanaAccount.IndexFormat
                    TraceIdQuery = kibanaAccount.TraceIdQuery
                    SearchSource = kibanaAccount.SearchSource
                }
                |> Some
            | _ -> None
        | _ -> None

    let setKibanaParams (m: KibanaSearchModel) = { m with SearchKibanaLogsParams = m |> toSearchKibanaLogsParams }

    let canMoveToPreviousPage (m: KibanaSearchModel) =
        match m.LogCountFrom, m.LogCountTo with
        | Some from, Some ``to`` when from > 0 -> true
        | _ -> false

    let withPreviousPage (m: KibanaSearchModel) =
        match m.LogCountFrom, m.LogCountTo with
        | Some from, Some ``to`` when from > 0 ->
            let from' = from - ``to``
            if from' < 0 then { m with LogCountFrom = None }
            else { m with LogCountFrom = from' |> Some }
        | _ -> m

    let canMoveToNextPage (m: KibanaSearchModel) =
        m.LogCountTo |> Option.isSome

    let withNextPage (m: KibanaSearchModel) =
        match m.LogCountFrom, m.LogCountTo with
        | Some from, Some ``to`` ->
            let from' = from + ``to``
            { m with LogCountFrom = from' |> Some }
        | None, Some ``to`` ->
            { m with LogCountFrom = ``to`` |> Some }
        | _ -> m


namespace LogParser.DesktopClient.ElmishApp.KibanaSearchModel

open System

module Program =

    open System.Windows
    open Microsoft.Extensions.Logging
    open Elmish
    open Elmish.Extensions
    open LogParser.Core
    open LogParser.DesktopClient.ElmishApp.Models
    open LogParser.DesktopClient.ElmishApp.Models.KibanaSearchModel
    open LogParser.DesktopClient.ElmishApp.Interfaces

    let update (settingsManager: ISettingsManager) (logger: ILogger) msg model =
        match msg with
        | SetSelectedKibanaAccountIndex ind ->
            model |> withSelectedKibanaAccountIndex ind |> setKibanaParams
            , Cmd.ofMsg Msg.SaveSelectedKibanaAccount
            , Intent.NoIntent

        | SetKibanaBaseUri uri -> model |> setKibanaBaseUri uri |> setKibanaParams, Cmd.ofMsg Msg.SaveKibanaBaseUri, Intent.NoIntent
        | SetKibanaLogin login -> model |> setKibanaLogin login |> setKibanaParams, Cmd.ofMsg Msg.SaveKibanaLogin, Intent.NoIntent
        | SetKibanaPassword pass -> model |> setKibanaPassword pass |> setKibanaParams, Cmd.none, Intent.NoIntent

        | SetTraceId traceId when traceId |> Option.exists (fun s -> not (String.IsNullOrWhiteSpace(s)) && not (s.StartsWith("m:"))) ->
            { model with TraceId = traceId } |> setKibanaParams, Cmd.none, Intent.NoIntent

        | SetTraceId traceId ->
            { model with TraceId = traceId; } |> setKibanaParams, Cmd.none, Intent.FilterOff

        | SetLogCount c -> model |> setLogCount c |> setKibanaParams, Cmd.none, Intent.FilterOff
        | SetLogCountFrom c -> model |> setLogCountFrom c |> setKibanaParams, Cmd.none, Intent.FilterOff
        | SetSelectedServiceName n -> model |> setSelectedServiceNames n |> setKibanaParams, Cmd.none, Intent.FilterOff

        | SetLogsDate logsDate -> { model with LogsDate = logsDate } |> setKibanaParams, Cmd.none, Intent.FilterOff
        | SetLogsStartTime logsStartTime -> { model with LogsStartTime = logsStartTime } |> setKibanaParams, Cmd.none, Intent.FilterOff
        | SetLogsEndTime logsEndTime -> { model with LogsEndTime = logsEndTime } |> setKibanaParams, Cmd.none, Intent.FilterOff

        | SearchKibanaLogs (Start ()) ->
            model
            , Cmd.OfTask.either 
                (Kibana.searchLogs logger) 
                (model.SearchKibanaLogsParams |> Option.get)
                (Finish >> SearchKibanaLogs)
                Msg.OnError
            , Intent.LoadingOn

        | SearchKibanaLogs (Finish logs) when logs |> (not << Seq.isEmpty) ->
            let logMessage = String.Join(Environment.NewLine, logs) |> Some
            model, Cmd.none, Intent.ProcessLoadedLogs logMessage

        | SearchKibanaLogs (Finish logs) when logs |> Seq.isEmpty ->
            let logMessage = "No Logs" |> Some
            model, Cmd.none, Intent.ProcessLoadedLogs logMessage

        | SaveKibanaBaseUri when model |> selectedKibanaBaseUri |> Option.isSome ->
            let accName = (model |> selectedKibanaAccountName).Value
            model|> selectedKibanaBaseUri |> Option.iter (fun s -> settingsManager.Save $"Kibana{accName}BaseUri" s)
            model, Cmd.none, Intent.FilterOff

        | SaveKibanaLogin when model |> selectedKibanaLogin |> Option.isSome ->
            let accName = (model |> selectedKibanaAccountName).Value
            model |> selectedKibanaLogin |> Option.iter (fun s -> settingsManager.Save $"Kibana{accName}Login" s)
            model, Cmd.none, Intent.FilterOff

        | SaveSelectedKibanaAccount when model.SelectedKibanaAccountIndex |> Option.isSome ->
            model.SelectedKibanaAccountIndex |> Option.iter (fun s -> settingsManager.Save "SelectedKibanaAccount" s)
            model, Cmd.none, Intent.FilterOff

        | CopyKibanaRequestToClipboard ->
            let copy kibanaParams =
                do Clipboard.SetText(Kibana.searchRequest kibanaParams)
            model, Cmd.OfFunc.attempt copy (model.SearchKibanaLogsParams |> Option.get) Msg.OnError, Intent.NoIntent

        | PreviousPage ->
            model |> withPreviousPage
            , model.SearchKibanaLogsParams |> Option.map (fun _ -> Cmd.ofMsg (SearchKibanaLogs (Start()))) |> Option.defaultValue Cmd.none
            , Intent.NoIntent

        | NextPage ->
            model |> withNextPage
            , model.SearchKibanaLogsParams |> Option.map (fun _ -> Cmd.ofMsg (SearchKibanaLogs (Start()))) |> Option.defaultValue Cmd.none
            , Intent.NoIntent

        | OnError err ->
            model, Cmd.none, Intent.OnError err

        | _ -> model, Cmd.none, Intent.FilterOff


module Bindings =

    open Elmish.WPF
    open Elmish.Extensions
    open LogParser.DesktopClient.ElmishApp.Models
    open LogParser.DesktopClient.ElmishApp.Models.KibanaSearchModel

    let bindings () : Binding<KibanaSearchModel, KibanaSearchModel.Msg> list =
        [
            "KibanaAccounts"
            |> Binding.oneWaySeq (
                (fun m -> m.KibanaAccounts),
                (fun a1 a2 -> a1.IndexPattern.Equals(a2.IndexPattern, StringComparison.Ordinal)),
                (fun a -> a.IndexPattern) 
            )

            "SelectedKibanaAccountIndex" |> Binding.twoWayOpt ((fun m -> m.SelectedKibanaAccountIndex), SetSelectedKibanaAccountIndex)

            "SelectedKibanaBaseUri" |> Binding.twoWayOpt (selectedKibanaBaseUri, SetKibanaBaseUri)
            "SelectedKibanaLogin" |> Binding.twoWayOpt (selectedKibanaLogin, SetKibanaLogin)
            "SelectedKibanaPassword" |> Binding.twoWayOpt (selectedKibanaPassword, SetKibanaPassword)

            "TraceId" |> Binding.twoWayOpt ((fun m -> m.TraceId), SetTraceId)
            "LogsDate" |> Binding.twoWayOpt ((fun m -> m.LogsDate), SetLogsDate)
            "LogsStartTime" |> Binding.twoWayOpt ((fun m -> m.LogsStartTime), SetLogsStartTime)
            "LogsEndTime" |> Binding.twoWayOpt ((fun m -> m.LogsEndTime), SetLogsEndTime)

            "ServiceNames" |> Binding.oneWaySeq (getServiceNames, (=), id)
            "SelectedServiceName" |> Binding.oneWayToSourceOpt (SetSelectedServiceName)

            "LogCountFrom" |> Binding.twoWayOpt (getLogCountFrom, SetLogCountFrom)
            "LogCountTo" |> Binding.twoWayOpt (getLogCount, SetLogCount)

            "SearchKibanaLogsCommand" 
            |> Binding.cmdIf (fun m ->
                m.SearchKibanaLogsParams
                |> Option.map (fun _ -> Start () |> SearchKibanaLogs)
            )

            "CopyKibanaRequestToClipboardCommand" 
            |> Binding.cmdIf (fun m ->
                m.SearchKibanaLogsParams
                |> Option.map (fun _ -> Msg.CopyKibanaRequestToClipboard)
            )

            "PreviousPageCommand" |> Binding.cmdIf (fun m -> if m |> canMoveToPreviousPage then PreviousPage |> Some else None)
            "NextPageCommand" |> Binding.cmdIf (fun m -> if m |> canMoveToNextPage then NextPage |> Some else None)
        ]

