namespace LogParser.App.TechnoField

open System
open LogParser.Core.Types
open Microsoft.Extensions.Logging

type Model =
    {
        TechnoField: TechnoField
        Key: string
        Header: string option
        Text: string option
        Json: string option
        Tag: Tag
    }
and
    Tag =
        | SimpleField = 0
        | JsonField = 1
        | AnnotatedJsonField = 2

type Msg = CopyValueRequested

module Program =

    open Elmish
    open Microsoft.FSharp.Reflection  


    let init (technoField: TechnoField) =

        

        match technoField with
        | TechnoField.Json (key, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
            }
        | TechnoField.Body fields ->
            { 
                TechnoField = technoField 
                Tag = Tag.JsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = None
            }
        | TechnoField.MessageBoddied (text, fields) ->
            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = "json object" |> Some; 
                Json = fields |> TechnoFields.toString 1 |> Some; 
                Text = text |> Some
            }

        | TechnoField.MessageParameterized (text, typeJson) ->
            let parameter = typeJson |> List.map (sprintf "%O") |> (fun l -> String.Join(",\n", l))

            { 
                TechnoField = technoField 
                Tag = Tag.AnnotatedJsonField; 
                Key = technoField |> TechnoFields.key; 
                Header = $"parameters:" |> Some; 
                Json = parameter |> Some; 
                Text = text |> Some
            }

        | _ ->
            { 
                TechnoField = technoField 
                Header = None; 
                Tag = Tag.SimpleField; 
                Key = technoField |> TechnoFields.key; 
                Text = technoField |> TechnoFields.value |> Some;
                Json = None
            }


    open System.Windows;

    let update msg model =
        match msg with
        | CopyValueRequested ->
            match model.Tag with
            | Tag.JsonField
            | Tag.AnnotatedJsonField ->
                Clipboard.SetText(model.Json |> Option.defaultValue "")
            | _ -> 
                Clipboard.SetText(model.Text |> Option.defaultValue "")

            model


    open Elmish.WPF

    let bindings () : Binding<Model, Msg> list =
        [
            "Tag" |> Binding.oneWay (fun m -> m.Tag)
            "Key" |> Binding.oneWay (fun m -> m.Key)
            "Text" |> Binding.oneWayOpt (fun m -> m.Text)
            "Header" |> Binding.oneWayOpt (fun m -> m.Header)
            "Json" |> Binding.oneWayOpt (fun m -> m.Json)
            "CopyCommand" |> Binding.cmd Msg.CopyValueRequested
        ]