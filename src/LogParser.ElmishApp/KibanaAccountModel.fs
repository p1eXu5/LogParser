namespace LogParser.ElmishApp.Models

open System
open p1eXu5.FSharp.ElmishExtensions
open LogParser.Kibana
open LogParser.ElmishApp.Helpers
open LogParser.ElmishApp.KibanaSearchModel.Services

type KibanaAccountModel =
    {
        Name: string
        BaseUri: string option
        Login: string option
        Password: string option
    }
    with
        override this.ToString() =
            this.BaseUri |> Option.defaultValue this.Name

module KibanaAccountModel =

    type Msg =
        | SetKibanaBaseUri of string option
        | SetKibanaLogin of string option
        | SetKibanaPassword of string option
