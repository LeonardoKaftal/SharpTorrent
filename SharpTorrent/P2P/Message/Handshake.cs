using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P.Message;

public static class Handshake
{
    private const int HandshakeLength = 68;

    public static async Task<byte[]> HandshakePeer(Socket peerSocket, byte[] infoHash, string peerId)
    {
        var sentHandshake = EncodeHandshake(infoHash, peerId);
        await peerSocket.SendAsync(sentHandshake);
        
        var receivedHandshake = new byte[68];

        var read = await peerSocket.ReceiveAsync(receivedHandshake, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        if (read < HandshakeLength) throw new FormatException($"Invalid handshake: received from peer {read} bytes instead of {HandshakeLength}");
        
        // don't count peer id
        receivedHandshake = receivedHandshake[0..(HandshakeLength - 20)];
        sentHandshake = sentHandshake[0..(HandshakeLength - 20)];

        if (sentHandshake.Equals(receivedHandshake))
            throw new FormatException("Invalid handshake: sent handshake is different from the one received");
        
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