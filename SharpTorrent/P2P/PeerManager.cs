using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerManager(
    ConcurrentDictionary<IPEndPoint,Peer> peers, 
    byte[][] pieces,
    byte[] infoHash,
    string peerId,
    ulong torrentLength,
    uint pieceLength)
{
    private readonly ConcurrentQueue<WorkPiece> _workQueue = new();
    
    
    public async Task Download()
    {
        for (var i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            var length = CalculatePieceLength(i);
            var workPiece = new WorkPiece(i, piece, length);
            _workQueue.Enqueue(workPiece);
        }

        var tasks = peers.Select(StartPeerTask).ToList();

        await Task.WhenAll(tasks); 
    }

    private async Task StartPeerTask(KeyValuePair<IPEndPoint,Peer> peer)
    {
        try
        {
            var peerConn = new PeerConnection(peer.Key);
            await peerConn.EstablishConnection(infoHash, peerId);
        }
        catch (Exception e)
        { 
            Singleton.Logger.LogWarning("Closing connection with peer {Ip} because of error {Error}", peer.Key.ToString(), e.Message);
            peer.Value.RemovePeer(peers); 
        }
    }


    // last piece could be truncated, it's needed to be calculated by hand in that case
    private uint CalculatePieceLength(int index)
    {
        var (begin, end) = CalculateBoundForPiece(index);
        return (uint)(int)(end - begin);
    }

    private Tuple<ulong, ulong> CalculateBoundForPiece(int index)
    {
        var begin = (ulong) index * pieceLength;
        var end = begin + pieceLength;

        if (end > torrentLength) end = torrentLength;
        return new Tuple<ulong, ulong>(begin, end);
    }
}