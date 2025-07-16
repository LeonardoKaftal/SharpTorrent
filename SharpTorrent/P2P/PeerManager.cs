using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SharpTorrent.Disk;
using SharpTorrent.P2P.Message;
using SharpTorrent.P2P.Piece;
using SharpTorrent.Torrent;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerManager(
    ConcurrentDictionary<IPEndPoint,Peer> peers, 
    byte[][] pieces,
    byte[] infoHash,
    string peerId,
    ulong torrentLength,
    uint pieceLength,
    List<TorrentFile> files)
{
    private const uint MaxBlockSize = 16384;
    private readonly ConcurrentQueue<PieceWork> _workQueue = new();
    private readonly DiskManager _diskManager = new(files,pieceLength);
    private int _downloadedPieces = 0;
    
    // last piece could be truncated, it's needed to be calculated by hand in that case
    private uint CalculatePieceLength(uint index)
    {
        var (begin, end) = CalculateBoundForPiece(index);
        return (uint)(int)(end - begin);
    }

    private Tuple<ulong, ulong> CalculateBoundForPiece(uint index)
    {
        var begin = (ulong) index * pieceLength;
        var end = begin + pieceLength;

        if (end > torrentLength) end = torrentLength;
        return new Tuple<ulong, ulong>(begin, end);
    }
    
    public async Task DownloadTorrent()
    {
        for (uint i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            var length = CalculatePieceLength(i);
            var workPiece = new PieceWork(i, piece, length);
            _workQueue.Enqueue(workPiece);
        }

        var tasks = peers.Select(StartPeerTask).ToList();

        await Task.WhenAll(tasks);
        _diskManager.Dispose();

        if (_downloadedPieces == pieces.Length)
        {
            Singleton.Logger.LogInformation("Successfully downloaded torrent, 100% download completed");
            
            // wait for disk manager to be disposed
            await Task.Delay(500);
            
            // sha256 log
            using var sha = SHA256.Create();
            foreach (var file in files)
            {
                var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var hash = await sha.ComputeHashAsync(stream);
                var computedHash = BitConverter
                    .ToString(hash)
                    .Replace("-", "")
                    .ToLowerInvariant();
                Singleton.Logger.LogInformation("SHA256 {Name}: {Hash}", file.FileName, computedHash);
            }
        }
        // failed download
        else
        {
            Singleton.Logger.LogCritical("Torrent download failed");
        }
    }

    private async Task StartPeerTask(KeyValuePair<IPEndPoint,Peer> peer)
    {
        using var peerConn = new PeerConnection(peer.Value);
        var unchockeMessage = new TorrentMessage(MessageType.Unchoke, []).Serialize();
        var interestedMessage = new TorrentMessage(MessageType.Interested, []).Serialize();
        PieceWork workPiece = null;

        try
        {
            await peerConn.EstablishConnection(infoHash, peerId);

            do
            {
                if (!_workQueue.TryDequeue(out var result))
                {
                    await Task.Delay(100);
                    continue;
                }

                workPiece = result;
                // get a piece that the peer have, then try to download it
                if (!Bitfield.HasPiece(peerConn.Bitfield, workPiece.Index))
                {
                    _workQueue.Enqueue(workPiece);
                    continue;
                }

                var haveMessage = TorrentMessage
                    .FormatHave(workPiece.Index)
                    .Serialize();

                await peerConn.SendMessageAsync(unchockeMessage);
                await peerConn.SendMessageAsync(interestedMessage);

                // 30 seconds are enough to download a piece
                var timer = Task.Delay(TimeSpan.FromSeconds(30));
                var pieceDownloadTask = AttemptDownloadPiece(workPiece, peerConn);

                var completedTask = await Task.WhenAny(timer, pieceDownloadTask);
                if (completedTask == timer)
                    throw new ProtocolViolationException(
                        $"Impossible to download piece {workPiece.Index} from peer because of timeout");

                var piece = await pieceDownloadTask;

                if (!VerifyHash(piece, workPiece))
                    throw new ProtocolViolationException("The downloaded piece have invalid hash!");

                var pieceResult = new PieceResult(workPiece.Index, piece);
                await peerConn.SendMessageAsync(haveMessage);

                // percentage
                _downloadedPieces = Interlocked.Increment(ref _downloadedPieces);
                var percentage = (double)_downloadedPieces / pieces.Length * 100;
                Singleton.Logger.LogInformation(
                    "Downloaded percentage {Percentage:F2}%, downloading from {PeerCount} peers", percentage, peers.Count);


                await _diskManager.WritePieceToDisk(pieceResult);
                workPiece = null;
            } while (!_workQueue.IsEmpty);
        }
        catch (Exception e)
        {
            Singleton.Logger.LogWarning("Closing connection with peer {Ip} because of error: {Error}", peer.Key.ToString(), e.Message);
            if (workPiece != null) _workQueue.Enqueue(workPiece);
            peer.Value.RemovePeer(peers);
        }
    }

    private bool VerifyHash(byte[] pieceByte, PieceWork pieceWork)
    {
        var computedHash = SHA1.HashData(pieceByte);
        return computedHash.SequenceEqual(pieceWork.Hash);
    }

    private async Task<byte[]> AttemptDownloadPiece(PieceWork workPiece, PeerConnection peerConnection)
    {
        var state = new PieceProgress(workPiece.Index, peerConnection, workPiece.Length);

        while (state.Downloaded < workPiece.Length)
        {
            while (!state.Connection.IsChocked
                   && state.Requested < workPiece.Length
                   && state.Backlog < state.Connection.ConnectedPeer.Backlog)
            {
                var blockSize = MaxBlockSize;
                if (workPiece.Length - state.Requested < MaxBlockSize)
                    blockSize = (uint)(int)(workPiece.Length - state.Requested);

                var requestMessage = TorrentMessage
                    .FormatRequest(workPiece.Index, state.Requested, blockSize)
                    .Serialize();

                await peerConnection.SendMessageAsync(requestMessage);
                state.Backlog++;
                state.Requested += blockSize;
            }
            await state.ReadState();
        }

        return state.Buff;
    }
}