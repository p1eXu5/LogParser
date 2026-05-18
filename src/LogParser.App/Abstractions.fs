namespace LogParser.App.Abstractions

open LogParser.App

type IStorage =
    interface
        abstract GetStream : unit -> Result<CancellableStream, string>
    end


type IStorageFactory<'Storage when 'Storage :> IStorage> =
    interface
        abstract Init : unit -> Result<'Storage, string> 
    end
