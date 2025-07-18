using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using SharpTorrent.Disk;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P.Piece;

public class PieceProgress(
    uint index,
    PeerConnection peerConnection,
    uint pieceSize
    )
{
    public readonly uint Index = index;
    public readonly PeerConnection Connection = peerConnection;
    public byte[] Buff { get; private set; } = new byte[pieceSize];
    public uint Requested = 0;
    public uint Downloaded = 0;
    public uint Backlog = 0;


    public async Task ReadState(DiskManager diskManager)
    {
        // blocking call
        var message = await Connection.ReadMessageAsync();

        switch (message.Type)
        {
            case MessageType.Choke:
                Connection.IsChocked = true;
                break;
            case MessageType.Unchoke:
                Connection.IsChocked = false;
                break;
            case MessageType.Have:
                var receivedIndex = message.ParseHave();
                Bitfield.SetPiece(Connection.Bitfield, receivedIndex);
                break;
            case MessageType.Piece:
                var n = message.ParsePiece(Index, Buff, message.Payload);
                Backlog--;
                Downloaded += n;
                Connection.ConnectedPeer.CalculateBacklog(n);
                break;
            case MessageType.Request:
                if (message.Payload.Length != 12) return;
                Singleton.Logger.LogWarning("Arrivata una request");
                var index = BinaryPrimitives.ReadUInt32BigEndian(message.Payload.AsSpan()[0..4]);
                var begin = BinaryPrimitives.ReadUInt32BigEndian(message.Payload.AsSpan()[4..8]);
                var length = BinaryPrimitives.ReadUInt32BigEndian(message.Payload.AsSpan()[8..12]);
                var piece = await diskManager.ReadPieceFromDisk(index, begin, length);
                if (piece.Buf.Length == 0) return;
                
                Singleton.Logger.LogInformation("Sending piece {piece} to peer {peer} after his request", index, peerConnection.ConnectedPeer.ToString());
                var pieceMessage = TorrentMessage.FormatPieceMessage(piece, begin);
                await peerConnection.SendMessageAsync(pieceMessage.Serialize());
                break;
        }
    }
}