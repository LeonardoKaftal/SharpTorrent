using System.Buffers.Binary;
using System.Net;

namespace SharpTorrent.P2P.Message;

public record TorrentMessage
{
    public MessageType Type { get; private set; }
    public readonly byte[] Payload;
    public readonly uint Length;
    
    
    public TorrentMessage(byte[] msg)
    {
        switch (msg.Length) {
            // keep alive
            case 0:
                Type = MessageType.KeepAlive;
                Payload = [];
                return;
            case < 4:
                throw new FormatException("Peer sent an invalid message, it's length is less than four");
        }
        
        Length = BinaryPrimitives.ReadUInt32BigEndian(msg.AsSpan()[0..4]);
        
        
        Type = (MessageType) msg[4];
        Payload = msg[5..];
    }

    public TorrentMessage(MessageType type, byte[] payload, uint length)
    {
        Type = type;
        Payload = payload;
        Length = length;
    }
    
    public byte[] Serialize()
    {
        var totalLength = 4 + Length;
        var messageBuff = new byte[totalLength];
        
        BinaryPrimitives.WriteUInt32BigEndian(messageBuff, Length);
        messageBuff[4] = (byte) Type;

        if (Payload.Length > 0)
        {
            Buffer.BlockCopy(Payload,
                0,
                messageBuff,
                5,
                Payload.Length
            ); 
        }
        return messageBuff;
    }

    public uint ParseHave()
    {
        if (Type != MessageType.Have)
            throw new ProtocolViolationException("Expected Have Message, got instead " + Type.ToString());
        return BinaryPrimitives.ReadUInt32BigEndian(Payload);
    }

    public int ParsePiece()
    {
        // todo
        throw new NotImplementedException();
    }
    
    // static formatting methods
    public static TorrentMessage FormatRequest(uint index, uint begin, uint length)
    {
        var payload = new byte[12];
        
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), index);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), begin);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), length);
        
        return new TorrentMessage(MessageType.Request,payload, 13);
    }

    public static TorrentMessage FormatCancel(uint index, uint begin, uint length)
    {
        var message = TorrentMessage.FormatRequest(index, begin, length);
        message.Type = MessageType.Cancel;
        return message;
    }

    public static TorrentMessage FormatHave(uint index)
    {
        var payload = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(payload, index);
        return new TorrentMessage(MessageType.Have, payload, 5);
    }
}