using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P.Message;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public class PeerConnection(IPEndPoint address): IDisposable
{
    private readonly TcpClient _tcpClient = new();
    public byte[] Bitfield { get; private set; } = [];
    public bool IsChocked;

    public async Task<TorrentMessage> ReadMessageAsync()
    {
        return await ReadMessageAsync(_tcpClient);
    }

    public async Task<TorrentMessage> ReadMessageAsync(TcpClient peerSocket)
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
    
    public async Task EstablishConnection(byte[] infoHash, string peerId)
    {
        Singleton.Logger.LogInformation("Trying to establish valid TCP connection with peer {Ip}", address.ToString());
        
        // tcp connection
        await _tcpClient.ConnectAsync(address, new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
        
        await HandshakePeer(_tcpClient.Client, infoHash, peerId);
        await ReceiveBitfield();
        
        Singleton.Logger.LogInformation("Successfully established connection with peer {Ip}", address.ToString());
    }

    private async Task HandshakePeer(Socket socket, byte[] infoHash, string peerId)
    {
        var timerTask = Task.Delay(TimeSpan.FromSeconds(5));
        Singleton.Logger.LogInformation("Trying to handshake peer {Ip}", address.ToString());
        
        var handshakeTask = Handshake.HandshakePeer(socket, infoHash, peerId);
        var finishedTask = await Task.WhenAny(timerTask, handshakeTask);
        
        if (finishedTask == timerTask) 
            throw new TimeoutException("Handshake timeout");
            
        await handshakeTask; 
        
        Singleton.Logger.LogInformation("Received valid handshake from peer {Ip}", address.ToString());
    }

    private async Task ReceiveBitfield()
    {
        Singleton.Logger.LogInformation("Trying to receive bitfield from {Ip}", address.ToString());
        
        TorrentMessage parsedMessage;
        do
        {
            parsedMessage = await ReadMessageWithTimerAsync(TimeSpan.FromSeconds(6));
        } 
        while (parsedMessage.Type == MessageType.KeepAlive);
        
        if (parsedMessage.Type != MessageType.Bitfield) 
            throw new ProtocolViolationException($"Expected Bitfield message but got {parsedMessage.Type}");
            
        Singleton.Logger.LogInformation("Successfully received Bitfield from {Ip}", address.ToString());
        Bitfield = parsedMessage.Payload;
    }
    
    public async Task SendMessageAsync(byte[] msg)
    {
        await _tcpClient
            .GetStream()
            .WriteAsync(msg);
    }

    public void Dispose()
    {
        _tcpClient?.Close();
        _tcpClient?.Dispose();
    }

}