using System.Collections.Concurrent;
using SharpTorrent.P2P.Piece;
using SharpTorrent.Torrent;

namespace SharpTorrent.Disk;

public class DiskManager(List<TorrentFile> files, uint pieceLength) : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _filesLocks = new();
    private readonly ConcurrentDictionary<string, FileStream> _filesStreams = new();
    public async Task WritePieceToDisk(PieceResult pieceResult)
    {
        var globalOffset = (ulong)pieceResult.Index * pieceLength;
        var remaining = (ulong) pieceResult.Buf.Length;
        ulong pieceBuffOffset = 0;

        foreach (var file in files)
        {
            if (globalOffset >= file.Length)
            {
                globalOffset -= file.Length;
                continue;
            }
            var toWrite = Math.Min(file.Length - globalOffset, remaining);
            var fileLock = _filesLocks.GetOrAdd(file.FilePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();
            
            try
            {
                var directory = Path.GetDirectoryName(file.FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
 
                var fs = _filesStreams.GetOrAdd(file.FilePath,new FileStream(file.FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write));
                fs.Seek((long)globalOffset, SeekOrigin.Begin);
                await fs.WriteAsync(pieceResult.Buf, (int)pieceBuffOffset, (int)toWrite);
            }
            finally
            {
                fileLock.Release();
            }

            pieceBuffOffset += toWrite;
            remaining -= toWrite;
            globalOffset = 0; 
            
            if (remaining == 0) break;
        }
    }

    public void Dispose()
    {
        foreach (var stream in _filesStreams.Values)
        {
            stream.Dispose();
        }
        _filesStreams.Clear();

        foreach (var semaphore in _filesLocks.Values)
        {
            semaphore.Dispose();
        }
        _filesLocks.Clear();
    }
}