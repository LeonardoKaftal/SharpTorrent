using System.Buffers.Binary;

namespace SharpTorrent.P2P.Message;

public record TorrentMessage
{
    public readonly MessageType Type;
    public readonly byte[] Payload;
    
    
    public TorrentMessage(byte[] msg)
    {
        if (msg.Length < 4) throw new FormatException("Peer sent an invalid message, it's length is less than four");
        var length = BinaryPrimitives.ReadUInt32BigEndian(msg.AsSpan()[0..4]);
        
        // keep alive
        if (length == 0)
        {
            Type = MessageType.KeepAlive;
            Payload = [];
            return;
        }
        
        Type = (MessageType) msg[4];
        Payload = msg[5..];
    }

}
