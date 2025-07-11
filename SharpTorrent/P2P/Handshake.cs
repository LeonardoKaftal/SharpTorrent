using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpTorrent.P2P;

public static class Handshake
{
    private const int HandshakeLength = 68;

    public static async Task<byte[]> HandshakePeer(Socket peerSocket, byte[] infoHash, string peerId)
    {
        var sentHandshake = EncodeHandshake(infoHash, peerId);
        await peerSocket.SendAsync(sentHandshake);
        
        var receivedHandshake = new byte[68];
        var totalRead = 0;
        
        while (totalRead < HandshakeLength)
        {
            var segment = new ArraySegment<byte>(receivedHandshake, totalRead, HandshakeLength - totalRead);
            var read = await peerSocket.ReceiveAsync(segment);
            if (read == 0) throw new ProtocolViolationException("Connection closed before handshake was complete");
            totalRead += read;

        }
        
        // check BitTorrent protocol string
        if (!receivedHandshake.AsSpan(0, 20).SequenceEqual(sentHandshake.AsSpan(0, 20)))
        {
            throw new FormatException("Invalid handshake: not bittorrent protocol");
        }
        
       // check infoHash 
       if (!receivedHandshake.AsSpan(28, 20).SequenceEqual(sentHandshake.AsSpan(28, 20)))
       {
            throw new FormatException("Invalid handshake: infohash does not match");
       }
        
        return receivedHandshake;
    }

    private static byte[] EncodeHandshake(byte[] infoHash, string peerId)
    {
        var handshakeBuffer = new byte[HandshakeLength];
        var pstr = "BitTorrent protocol"u8.ToArray();

        handshakeBuffer[0] = 19;
        Buffer.BlockCopy(pstr, 0, handshakeBuffer, 1, 19);
        Buffer.BlockCopy(infoHash, 0, handshakeBuffer, 28, 20);
        Buffer.BlockCopy(Encoding.UTF8.GetBytes(peerId), 0, handshakeBuffer, 48, 20);

        return handshakeBuffer;
    }
}