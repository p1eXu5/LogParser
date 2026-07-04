namespace LogParser.App

open System.IO
open System.Threading

type CancellableStream (wrappedStream: Stream, ?cancellationToken: CancellationToken) =
    inherit Stream()
    
    let ct = defaultArg cancellationToken CancellationToken.None
    
    override this.CanRead = wrappedStream.CanRead
    override this.CanWrite = wrappedStream.CanWrite
    override this.CanSeek = wrappedStream.CanSeek
    override this.Length = wrappedStream.Length
    
    override this.Position
        with get () = wrappedStream.Position
        and set value = wrappedStream.Position <- value
    
    override this.Flush() =
        ct.ThrowIfCancellationRequested()
        wrappedStream.Flush()
    
    override this.Seek(offset: int64, origin: SeekOrigin): int64 =
        ct.ThrowIfCancellationRequested()
        wrappedStream.Seek(offset, origin)
    
    override this.SetLength(value: int64): unit =
        ct.ThrowIfCancellationRequested()
        wrappedStream.SetLength(value)
    
    override this.Read(buffer: byte[], offset: int, count: int): int =
        ct.ThrowIfCancellationRequested()
        wrappedStream.Read(buffer, offset, count)
    
    override this.Write(buffer: byte[], offset: int, count: int): unit =
        ct.ThrowIfCancellationRequested()
        wrappedStream.Write(buffer, offset, count)
    
    override this.Dispose(disposing: bool): unit =
        if disposing then
            wrappedStream.Dispose()
        base.Dispose(disposing)