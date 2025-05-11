using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerConnection(KeyValuePair<IPEndPoint, Peer> peer, byte[] infoHash, string peerId)
{
    private readonly TcpClient _tcpClient = new();
    
    public async Task EstablishConnection()
    {
        var ip = peer.Value.Ip.ToString();
        
        Singleton.Logger.LogInformation("Trying to establish valid TCP connection with peer {Ip}", ip);
         
        // connect with timer of 3 second
        var connectionTask = _tcpClient.ConnectAsync(peer.Key);
        if (await Task.WhenAny(connectionTask, Task.Delay(TimeSpan.FromSeconds(3))) != connectionTask) 
            throw new ProtocolViolationException("impossible to establish TCP connection with peer because it did not respond");
        
        // handshake
        await HandshakePeer(_tcpClient.Client, ip);
    }

    private async Task HandshakePeer(Socket socket, string ip)
    {
        Singleton.Logger.LogInformation("Trying to handshake peer {Ip}", ip);
        await Handshake.HandshakePeer(socket, infoHash, peerId);
        Singleton.Logger.LogInformation("Received valid handshake from peer {Ip}", ip);
    }

    public async Task StartDownloadTask(int pieceLength)
    {
        var bitfield = await ReceiveBitfield(pieceLength);
        Singleton.Logger.LogInformation("Successfully received BITFIELD");
    }

    private async Task<TorrentMessage> ReceiveBitfield(int piecesLength)
    {
        var pieceNumber = piecesLength / 8 + 1;
        var bitfieldBuff = new byte[pieceNumber];
        await _tcpClient.Client.ReceiveAsync(bitfieldBuff, new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

        var parsedMessage = new TorrentMessage(bitfieldBuff);

        if (parsedMessage.Type != MessageType.Bitfield) throw new ProtocolViolationException("Was expected a message with bitfield but got instead " + parsedMessage.Type.ToString());
        return parsedMessage;
    }
}