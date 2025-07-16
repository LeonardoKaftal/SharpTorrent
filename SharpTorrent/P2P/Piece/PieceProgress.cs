using Microsoft.Extensions.Logging;
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


    public async Task ReadState()
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
            case MessageType.Cancel:
                break;
        }
    }
}