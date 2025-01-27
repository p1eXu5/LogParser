namespace LogParser.DesktopClient.ElmishApp.Models

open System

type FiltersModel =
    {
        ServiceNames: string list
        SelectedServiceName: string option
        LogLevels: string list
        SelectedLogLevel: string option
        FilterOn: bool
        ShowInnerHierarchyLogs: bool
        TraceId: string option
        Start: DateTime option
        End: DateTime option
    }

module FiltersModel =

    type Msg =
        | SetServiceNames of string list
        | SetSelectedServiceName of string option
        | SetLogLevels of string list
        | SetSelectedLogLevel of string option
        | ToggleFilterOn of bool
        | ToggleShowInnerHierarchyLogs of bool
        | SetStartDate of DateTime option
        /// does not switch on filter
        | SetStartDateNoActivate of DateTime option
        | SetEndDate of DateTime option
        /// does not switch on filter
        | SetEndDateNoActivate of DateTime option

    type Intent =
        | OrderByTimestamp

    let init () =
        {
            ServiceNames = []
            SelectedServiceName = None
            LogLevels = []
            SelectedLogLevel = None
            FilterOn = false
            ShowInnerHierarchyLogs = true
            TraceId = None
            Start = None
            End = None
        }

    // ------------------------------ accessors

    open LogParser.DesktopClient.ElmishApp.Helpers

    let getServiceNames m = m.ServiceNames
    let setServiceNames serviceNames m =
        { m with ServiceNames = serviceNames }

    let setSelectedServiceName serviceName m =
        match serviceName with
        | Some sn when sn |> notEmpty ->
            { m with SelectedServiceName = serviceName }
        | _ -> { m with SelectedServiceName = None }

    // ------------------------------ 
    let getLogLevels m = m.LogLevels
    let setLogLevels logLevels m =
        { m with LogLevels = logLevels }

    let setSelectedLogLevel logLevel m =
        match logLevel with
        | Some ll when ll |> notEmpty ->
            { m with SelectedLogLevel = logLevel }
        | _ -> { m with SelectedLogLevel = None }

    // ------------------------------ 
    let startDate (m: FiltersModel) = m.Start
    let withStartDate v (m: FiltersModel) = { m with Start = v }

    let endDate (m: FiltersModel) = m.End
    let withEndDate v (m: FiltersModel) = { m with End = v }

    let withToggleOn (m: FiltersModel) =
        if m.FilterOn then m
        else { m with FilterOn = true }

namespace LogParser.DesktopClient.ElmishApp.FiltersModel


module Program =

    open Elmish
    open LogParser.DesktopClient.ElmishApp.Models
    open LogParser.DesktopClient.ElmishApp.Models.FiltersModel

    let update msg model =
        match msg with
        | ToggleFilterOn v -> { model with FilterOn = v }
        | ToggleShowInnerHierarchyLogs v -> { model with ShowInnerHierarchyLogs = v }
        | SetServiceNames names -> model |> setServiceNames names
        | SetSelectedServiceName n -> model |> setSelectedServiceName n |> withToggleOn
        | SetLogLevels logLevels -> model |> setLogLevels logLevels
        | SetSelectedLogLevel l -> model |> setSelectedLogLevel l |> withToggleOn
        | SetStartDate dt -> model |> withStartDate dt |> withToggleOn
        | SetStartDateNoActivate dt -> model |> withStartDate dt
        | SetEndDate dt -> model |> withEndDate dt |> withToggleOn
        | SetEndDateNoActivate dt -> model |> withEndDate dt


module Bindings =

    open Elmish.WPF
    open LogParser.DesktopClient.ElmishApp.Models
    open LogParser.DesktopClient.ElmishApp.Models.FiltersModel

    let bindings () : Binding<FiltersModel, FiltersModel.Msg> list =
        [
            "ShowInnerHierarchyLogs" |> Binding.twoWay ((fun m -> m.ShowInnerHierarchyLogs), Msg.ToggleShowInnerHierarchyLogs) // TODO: remove after make log model hierarchy

            "FilterOn" |> Binding.twoWay ((fun m -> m.FilterOn), ToggleFilterOn)

            "ServiceNames" |> Binding.oneWaySeq (getServiceNames, (=), id)
            "SelectedServiceName" |> Binding.oneWayToSourceOpt (SetSelectedServiceName)

            "LogLevels" |> Binding.oneWaySeq (getLogLevels, (=), id)
            "SelectedLogLevel" |> Binding.oneWayToSourceOpt (SetSelectedLogLevel)

            "Start" |> Binding.twoWayOpt (startDate, SetStartDate)
            "End" |> Binding.twoWayOpt (endDate, SetEndDate)
        ]