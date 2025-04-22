using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerManager(ConcurrentDictionary<IPEndPoint, Peer> peers, byte[] infoHash, string peerId)
{
    public async Task<byte[]> Download()
    {
        List<Task> peersTask = [];
        peersTask.AddRange(peers.Select(StartPeerTask));

        await Task.WhenAll(peersTask);
        return null;
    }

    private async Task StartPeerTask(KeyValuePair<IPEndPoint, Peer> peer)
    {
        var ip = peer.Value.Ip.ToString();
        
        try
        {
            using var tcpClient = new TcpClient();
            Singleton.Logger.LogInformation("Trying to establish valid TCP connection with peer {Ip}", ip);
            var timer = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
             
            // connect with timer of 3 second
            await tcpClient.ConnectAsync(peer.Key, timer);
            if (!tcpClient.Connected) throw new ProtocolViolationException("impossible to establish TCP connection with peer");
            
            // handshake
            await HandshakePeer(tcpClient.Client, ip);
            
            
        }
        catch (Exception e)
        {
            var removed = peers.Remove(peer.Key, out _);
            Singleton.Logger.LogWarning("Closing connection with peer {Ip} because of error {Error}", ip, e.Message);
            if (!removed) Singleton.Logger.LogCritical("CRITICAL ERROR, CLIENT HAS NOT BEEN ABLE TO REMOVE PEER {Ip} THAT SHOULD HAVE BEEN PRESENT", ip );
        }
    }

    private async Task HandshakePeer(Socket socket, string ip)
    {
        Singleton.Logger.LogInformation("Trying to handshake peer {Ip}", ip);
        await Handshake.HandshakePeer(socket, infoHash, peerId);
        Singleton.Logger.LogInformation("Received valid handshake from peer {Ip}", ip);
    }
}