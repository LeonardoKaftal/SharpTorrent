using System;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.P2P;
using SharpTorrent.P2P.Message;
using Xunit;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tests.P2P;

[TestSubject(typeof(TorrentMessage))]
public class MessageTest
{
    [Fact]
    public void TorrentMessage_TestConstructor_ReturnTorrentMessage()
    {
        byte[] payload = [0, 0, 0, 0, 0, 1, 1, 0, 1, 0];
        var expected = new TorrentMessage(MessageType.Bitfield, payload);

        var actual = new TorrentMessage(
            ReverseIfLittleEndian(BitConverter.GetBytes(payload.Length + 1))
                .Concat([(byte) MessageType.Bitfield])
                .Concat(payload)
                .ToArray());

        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void TorrentMessage_TestHaveMessage_ReturnTorrentMessage()
    {
        // "Have" message → ID = 4, payload = 4 byte 
        const int pieceIndex = 123;
        var payload = ReverseIfLittleEndian(BitConverter.GetBytes((pieceIndex)));

        const uint length = 5;

        //  [length (4 byte)] + [ID = 4 (1 byte)] + [payload (4 byte)]
        var fullMessage =
            ReverseIfLittleEndian(BitConverter.GetBytes(((int)length))) 
            .Concat(new byte[] { 4 }) // Message ID for "Have"
            .Concat(payload)          // Payload (piece index)
            .ToArray();

        var expected = new TorrentMessage(
            MessageType.Have,
            payload
        );

        var actual = new TorrentMessage(fullMessage);

        actual.Should().BeEquivalentTo(expected);
    }
    
    
    [Fact]
    public void TorrentMessage_TestConstructorWithEmptyPayload_ReturnTorrentMessage()
    {
        // given
        byte[] payload = [5, 0, 0, 0, 0, 1, 1, 0, 1, 0];
        var expected = new TorrentMessage(MessageType.Bitfield, payload[1..]);
        
        // when
        var actual = new TorrentMessage(
            ReverseIfLittleEndian(BitConverter.GetBytes(payload.Length))
                .Concat(payload)
                .ToArray());
        
        // then
        actual.Should().BeEquivalentTo(expected);
    }


    [Fact]
    public async Task PeerConnection_TestReadMessageAsync_ReturnBytes()
    {
        // peer setup
        // given
        var torrentMessage = new TorrentMessage(MessageType.Have, [0,0,0,4]);
        // the peer that write the message
        var peerClient = new TcpClient();
        // the client that receive the message
        var tcpListener = new TcpListener(IPAddress.Loopback, 8080);
        // extracted index from have message
        const uint expected = 4;
        
        //when
        tcpListener.Start();
        await peerClient.ConnectAsync(IPAddress.Loopback, 8080);
        var acceptedPeerSocket = await tcpListener.AcceptTcpClientAsync();
         
        await peerClient
            .GetStream()
            .WriteAsync(torrentMessage.Serialize());
        var actual = await PeerConnection.ReadMessageAsync(acceptedPeerSocket);
        
        // then
        actual.ParseHave().Should().Be(expected);
        tcpListener.Dispose();
        peerClient.Dispose();
    }

    [Fact]
    public void TorrentMessage_TestParsePiece_ReturnNBytesRead()
    {
        // given
        var random = new Random();
        const uint index = 4;
        const uint pieceLength = 64;
        const uint begin = 14;
        const uint blockSize = 256;
        
        var buff = new byte[blockSize];
        var pieceMessagePayload = new byte[pieceLength];
        
        BinaryPrimitives.WriteUInt32BigEndian(pieceMessagePayload, index);
        BinaryPrimitives.WriteUInt32BigEndian(pieceMessagePayload.AsSpan(4,4), begin); 
        
        var pieceMessage = new TorrentMessage(MessageType.Piece, pieceMessagePayload);
        random.NextBytes(pieceMessagePayload.AsSpan()[8..]);

        var n = pieceMessage.ParsePiece(index, buff, pieceMessagePayload);
        var expectedBytesDownloaded = (uint) pieceMessagePayload.Length - 8;
        n.Should().Be(expectedBytesDownloaded);
    }
}