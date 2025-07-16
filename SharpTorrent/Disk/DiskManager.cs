using System.Collections.Concurrent;
using SharpTorrent.P2P.Piece;
using SharpTorrent.Torrent;

namespace SharpTorrent.Disk;

public class DiskManager : IDisposable
{
    private readonly List<TorrentFile> _files;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _filesLocks = new();
    private readonly ConcurrentDictionary<string, FileStream> _filesStreams = new();
    
    // state file is an hidden file saved in the download files folder, it's needed to resume download if it got interrupted 
    private readonly FileStream _stateFileStream;
    private readonly SemaphoreSlim _stateLock = new(1,1);

    public DiskManager(List<TorrentFile> files, string pathForStateFile)
    {
        this._files = files;
        var folder = Path.GetDirectoryName(pathForStateFile);

        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        this._stateFileStream = File.Open(pathForStateFile, FileMode.OpenOrCreate);
        if (OperatingSystem.IsWindows()) File.SetAttributes(pathForStateFile, FileAttributes.Hidden);
    }

    public async Task WritePieceToDisk(PieceResult pieceResult, uint pieceLength)
    {
        var globalOffset = (ulong)pieceResult.Index * pieceLength;
        var remaining = (ulong) pieceResult.Buf.Length;
        ulong pieceBuffOffset = 0;

        foreach (var file in _files)
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
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var fs = _filesStreams.GetOrAdd(file.FilePath,new FileStream(file.FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write));
                fs.Seek((long)globalOffset, SeekOrigin.Begin);
                await fs.WriteAsync(pieceResult.Buf, (int)pieceBuffOffset, (int)toWrite);
                
            }
            finally
            {
                // write to the state file to resume download if interrupted
                fileLock.Release();
                try
                {
                    await _stateLock.WaitAsync();
                    _stateFileStream.Seek(pieceResult.Index, SeekOrigin.Begin);
                    _stateFileStream.WriteByte(1);
                }
                finally
                {
                    _stateLock.Release();
                }
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
        
        _stateFileStream.Dispose();
        _stateLock.Dispose();
    }

    // not thread safe
    public bool StateFileContainsPiece(long index)
    {
        if (_stateFileStream.Length < index) return false;
        _stateFileStream.Seek( index,SeekOrigin.Begin);
        return _stateFileStream.ReadByte() == 1;
    }
}