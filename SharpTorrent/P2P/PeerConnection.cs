using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerConnection(Peer peer): IDisposable
{
    private readonly TcpClient _peerSocket = new();
    public readonly Peer ConnectedPeer = peer;
    public byte[] Bitfield { get; private set; } = [];
    public bool IsChocked;

    public async Task<TorrentMessage> ReadMessageAsync()
    {
        return await ReadMessageAsync(_peerSocket);
    }

    public static async Task<TorrentMessage> ReadMessageAsync(TcpClient peerSocket)
    {
        var lengthBuff = new byte[4];
        var received = 0;
        
        while (received < 4)
        {
            var segment = new ArraySegment<byte>(lengthBuff, received, lengthBuff.Length - received); 
            var bytesRead = await peerSocket
                .GetStream()
                .ReadAsync(segment);
            
            if (bytesRead == 0)
                throw new InvalidOperationException("Connection closed by peer");
                
            received += bytesRead;
        }

        var messageLength = BinaryPrimitives.ReadUInt32BigEndian(lengthBuff);
        
        // keep alive
        if (messageLength == 0) return new TorrentMessage(lengthBuff);

        var messageBuff = new byte[messageLength];
        received = 0;
        
        while (received < messageLength)
        {
            var segment = new ArraySegment<byte>(messageBuff, received, (int) messageLength - received);
            var bytesRead = await peerSocket
                .GetStream()
                .ReadAsync(segment);
            
            
            if (bytesRead == 0)
                throw new ProtocolViolationException("Connection closed by peer");
                
            received += bytesRead;
        }
        
        var fullMessage = new byte[4 + messageLength];
        Array.Copy(lengthBuff, 0, fullMessage, 0, 4);
        Array.Copy(messageBuff, 0, fullMessage, 4, (int)messageLength);
        
        return new TorrentMessage(fullMessage);    
    }

    public async Task<TorrentMessage> ReadMessageWithTimerAsync(TimeSpan timeout)
    {
        var timerTask = Task.Delay(timeout);
        var readMessageTask = ReadMessageAsync();

        // read message with timer
        var finishedTask = await Task.WhenAny(timerTask, readMessageTask);

        if (finishedTask == timerTask) 
            throw new TimeoutException("Timeout while reading message");
            
        return await readMessageTask; 
    }
    
    public async Task EstablishConnection(byte[] infoHash, string clientPeerId)
    {
        Singleton.Logger.LogInformation("Trying to establish valid TCP connection with peer {Ip}", ConnectedPeer.ToString());
        
        // tcp connection with timer of 5 seconds
        var timer = Task.Delay(TimeSpan.FromSeconds(5));
        var connTask = _peerSocket.ConnectAsync(ConnectedPeer.Ip, ConnectedPeer.Port);

        var completedTask = await Task.WhenAny(connTask, timer);
        if (completedTask == timer) throw new ProtocolViolationException($"Can't connect with peer {ConnectedPeer.ToString()} because of timeout");

        await connTask;
        await HandshakePeer(_peerSocket.Client, infoHash, clientPeerId);
        await ReceiveBitfield();
        
        Singleton.Logger.LogInformation("Successfully established connection with peer {Ip}", ConnectedPeer.ToString());
    }

    private async Task HandshakePeer(Socket socket, byte[] infoHash, string peerId)
    {
        var timerTask = Task.Delay(TimeSpan.FromSeconds(5));
        Singleton.Logger.LogInformation("Trying to handshake peer {Ip}", ConnectedPeer.ToString());
        
        var handshakeTask = Handshake.HandshakePeer(socket, infoHash, peerId);
        var finishedTask = await Task.WhenAny(timerTask, handshakeTask);
        
        if (finishedTask == timerTask) 
            throw new TimeoutException("Handshake timeout");
            
        await handshakeTask; 
        
        Singleton.Logger.LogInformation("Received valid handshake from peer {Ip}", ConnectedPeer.ToString());
    }

    private async Task ReceiveBitfield()
    {
        Singleton.Logger.LogInformation("Trying to receive bitfield from {Ip}", ConnectedPeer.ToString());
        
        TorrentMessage parsedMessage;
        do
        {
            parsedMessage = await ReadMessageWithTimerAsync(TimeSpan.FromSeconds(6));
        } 
        while (parsedMessage.Type == MessageType.KeepAlive);
        
        if (parsedMessage.Type != MessageType.Bitfield) 
            throw new ProtocolViolationException($"Expected Bitfield message but got {parsedMessage.Type}");
            
        Singleton.Logger.LogInformation("Successfully received Bitfield from {Ip}", ConnectedPeer.ToString());
        Bitfield = parsedMessage.Payload;
    }
    
    public async Task SendMessageAsync(byte[] msg)
    {
        await _peerSocket
            .GetStream()
            .WriteAsync(msg);
    }

    public void Dispose()
    {
        _peerSocket?.Close();
        _peerSocket?.Dispose();
    }

}