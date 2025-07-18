using System.Collections.Concurrent;
using SharpTorrent.P2P;
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
    public readonly byte[] MyBitfield;
    private readonly uint _pieceLength;

    public DiskManager(List<TorrentFile> files, string pathForStateFile, uint piecesLength, uint pieceLength)
    {
        _files = files;
        _pieceLength = pieceLength;
        var folder = Path.GetDirectoryName(pathForStateFile);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        _stateFileStream = File.Open(pathForStateFile, FileMode.OpenOrCreate);
        if (OperatingSystem.IsWindows()) File.SetAttributes(pathForStateFile, FileAttributes.Hidden);
        MyBitfield = ReadState(piecesLength);
    }

    public async Task WritePieceToDisk(PieceResult pieceResult)
    {
        var globalOffset = (ulong)pieceResult.Index * _pieceLength;
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
                await fs.FlushAsync();
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

        // Write to the state file to resume download if interrupted
        await _stateLock.WaitAsync();
        try
        {
            // Set the bit corresponding to the completed piece
            Bitfield.SetPiece(MyBitfield, pieceResult.Index);
            
            // Save the entire bitfield to the state file
            _stateFileStream.Seek(0, SeekOrigin.Begin);
            await _stateFileStream.WriteAsync(MyBitfield, 0, MyBitfield.Length);
            await _stateFileStream.FlushAsync();
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private byte[] ReadState(uint piecesLength)
    {
        // Calculate the correct bitfield size in bytes
        var bitfieldSizeInBytes = (int)Math.Ceiling(piecesLength / 8.0);
        var bitfield = new byte[bitfieldSizeInBytes];
        
        _stateFileStream.Seek(0, SeekOrigin.Begin);
        
        // Read the bitfield from the file (if it exists and has the right size)
        if (_stateFileStream.Length >= bitfieldSizeInBytes)
        {
            _stateFileStream.Read(bitfield, 0, bitfieldSizeInBytes);
        }
        
        return bitfield;
    }
   
    // return the piece from disk, if it does not exist return an empty piece
    public async Task<PieceResult> ReadPieceFromDisk(uint index, uint begin, uint length)
    {
        // failure, piece it's not present on disk
        if (!Bitfield.HasPiece(MyBitfield, index)) return new PieceResult(index, []);

        var buffer = new byte[length];
        var globalOffset = (ulong)index * _pieceLength + begin;
        ulong remaining = length;
        ulong pieceBuffOffset = 0;

        foreach (var file in _files)
        {
            if (globalOffset >= file.Length)
            {
                globalOffset -= file.Length;
                continue;
            }

            var toRead = Math.Min(file.Length - globalOffset, remaining);
            var fileLock = _filesLocks.GetOrAdd(file.FilePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync();

            try
            {
                var fs = _filesStreams.GetOrAdd(file.FilePath,
                    new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read));

                fs.Seek((long)globalOffset, SeekOrigin.Begin);
                var read = await fs.ReadAsync(buffer, (int)pieceBuffOffset, (int)toRead);

                // If the file is too short or corrupted, return empty result
                if (read != (int)toRead)
                {
                    return new PieceResult(index, []);
                }
            }
            finally
            {
                fileLock.Release();
            }

            pieceBuffOffset += toRead;
            remaining -= toRead;
            globalOffset = 0;

            if (remaining == 0) break;
        }

        return new PieceResult(index, buffer);
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
}