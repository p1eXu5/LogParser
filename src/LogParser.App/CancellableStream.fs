namespace LogParser.App

open System.IO
open System.Threading

type CancellableStream (fileStream: FileStream, ?cancellationToken: CancellationToken) =
    inherit Stream()
    
    let ct = defaultArg cancellationToken CancellationToken.None
    
    override this.CanRead = fileStream.CanRead
    override this.CanWrite = fileStream.CanWrite
    override this.CanSeek = fileStream.CanSeek
    override this.Length = fileStream.Length
    
    override this.Position
        with get () = fileStream.Position
        and set value = fileStream.Position <- value
    
    override this.Flush() =
        ct.ThrowIfCancellationRequested()
        fileStream.Flush()
    
    override this.Seek(offset: int64, origin: SeekOrigin): int64 =
        ct.ThrowIfCancellationRequested()
        fileStream.Seek(offset, origin)
    
    override this.SetLength(value: int64): unit =
        ct.ThrowIfCancellationRequested()
        fileStream.SetLength(value)
    
    override this.Read(buffer: byte[], offset: int, count: int): int =
        ct.ThrowIfCancellationRequested()
        fileStream.Read(buffer, offset, count)
    
    override this.Write(buffer: byte[], offset: int, count: int): unit =
        ct.ThrowIfCancellationRequested()
        fileStream.Write(buffer, offset, count)
    
    override this.Dispose(disposing: bool): unit =
        if disposing then
            fileStream.Dispose()
        base.Dispose(disposing)