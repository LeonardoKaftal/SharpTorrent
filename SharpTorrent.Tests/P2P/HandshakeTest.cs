using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.P2P;
using Xunit;

namespace SharpTorrent.Tests.P2P;

[TestSubject(typeof(Handshake))]
public class HandshakeTest
{
    // this is a listener that is mocking another peer that send back a handshake
    private readonly TcpListener _listener =
        new(IPAddress.Loopback, 3000);
    
    [Fact]
    public async Task Handshake_HandshakePeer_ReturnBytes()
    {
        // server initialization
        _listener.Start();
        var clientSocketTask = _listener.AcceptSocketAsync();
        
        // given
        byte[] infoHash = [
            0x86, 0xD4, 0xC8, 0x00, 0x24, 0xA4, 0x69, 0xBE,
            0x4C, 0x50, 0xBC, 0x5A, 0x10, 0x2C, 0xF7, 0x17, // info hash
            0x80, 0x31, 0x00, 0x74
        ];

        const string peerId = "-TR2940-k8hj0wgej6ch";

        var expectedHandshake = new byte[]
        {
            0x13, // length of protocol string
            (byte)'B', (byte)'i', (byte)'t', (byte)'T', (byte)'o', (byte)'r', (byte)'r', (byte)'e',
            (byte)'n', (byte)'t', (byte)' ', (byte)'p', (byte)'r', (byte)'o', (byte)'t', (byte)'o',
            (byte)'c', (byte)'o', (byte)'l',
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // reserved
        }
        .Concat(infoHash)
        .Concat(Encoding.UTF8.GetBytes(peerId))
        .ToArray();
        
        // when
        var serverSocket = new TcpClient();
        // torrent client connect to the peer
        await serverSocket.ConnectAsync(IPAddress.Loopback, 3000);
        
        // peer accept connection and send handshake 
        var clientSocket = await clientSocketTask;
        await clientSocket.SendAsync(expectedHandshake);
        
        // then 
        var actual = () => Handshake.HandshakePeer(serverSocket.Client, infoHash, peerId);
        await actual.Should().NotThrowAsync();
        serverSocket.Dispose();
        clientSocket.Dispose();
    }
}