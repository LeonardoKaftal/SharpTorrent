using System.Buffers.Binary;
using System.Net;
using SharpTorrent.P2P.Piece;

namespace SharpTorrent.P2P.Message;

public record TorrentMessage
{
    public MessageType Type { get; private set; }
    public readonly byte[] Payload = [];
    private readonly uint _length;
    
    
    public TorrentMessage(byte[] msg)
    {
        if (msg.Length < 4)
        {
            throw new FormatException("Peer sent an invalid message, it's length is less than four");
        }

        _length = BinaryPrimitives.ReadUInt32BigEndian(msg.AsSpan()[0..4]);
        if (_length == 0)
        {
            Type = MessageType.KeepAlive;
            return;
        }

        if (msg.Length < 5) throw new FormatException($"Message claims length {_length} but actual message is too short to contain type byte");
        
        Type = (MessageType) msg[4];
        if (msg.Length > 5) Payload = msg[5..];
    }

    public TorrentMessage(MessageType type, byte[] payload)
    {
        Type = type;
        Payload = payload;
        _length = (uint)(Payload.Length + 1);
    }
    
    public byte[] Serialize()
    {
        var totalLength = 4 + _length;
        var messageBuff = new byte[totalLength];
        
        BinaryPrimitives.WriteUInt32BigEndian(messageBuff, _length);
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
        if (Payload.Length != 4)
            throw new ProtocolViolationException(
                "Invalid Have Message, expected payload with 4 bytes but got instead " + Payload.Length);
        return BinaryPrimitives.ReadUInt32BigEndian(Payload);
    }

    public uint ParsePiece(uint index, byte[] buffer, byte[] payload)
    {
        if (Type != MessageType.Piece)
            throw new ProtocolViolationException("Expected Piece Message, got instead " + Type.ToString());
        if (payload.Length < 8)
            throw new ProtocolViolationException(
                "Invalid piece message, expected payload with at least 8 bytes but got instead " + payload.Length);

        var parsedIndex = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
        if (parsedIndex != index)
            throw new ProtocolViolationException("Invalid Piece Message, parsed index is different from the one expected");
        var begin = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(4, 4));
        
        var data = payload[8..];
        if (begin > buffer.Length || begin + data.Length > buffer.Length)
            throw new ProtocolViolationException("Invalid Piece Message, begin and data exceed buffer capacity");
        
        var n = data.Length;
        
        Buffer.BlockCopy(data,0, buffer, begin, n);
        return (uint)n;
    }
    
    // static formatting methods
    public static TorrentMessage FormatRequest(uint index, uint begin, uint length)
    {
        var payload = new byte[12];
        
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), index);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), begin);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), length);
        
        return new TorrentMessage(MessageType.Request,payload);
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
        return new TorrentMessage(MessageType.Have, payload);
    }
    
    
    public static TorrentMessage FormatPieceMessage(PieceResult piece, uint begin)
    {
        var buff = new byte[8 + piece.Buf.Length];
        BinaryPrimitives.WriteUInt32BigEndian(buff.AsSpan(0, 4), piece.Index);
        BinaryPrimitives.WriteUInt32BigEndian(buff.AsSpan(4, 4), begin);
        piece.Buf.CopyTo(buff, 8);
        return new TorrentMessage(MessageType.Piece, buff);
    }
}