using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.P2P;
using SharpTorrent.Tracker.Udp;
using Xunit;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tests.Tracker;

[TestSubject(typeof(UdpTrackerAnnounceResponse))]
public class UdpTrackerAnnounceResponseTest
{
    [Fact]
    public void UdpTrackerAnnounceResponse_TestConstructorWithValidResponseBytes_ReturnAnnounceResponse()
    {
        var myTransactionId = new Random().Next();
        var expectedPeers = new ConcurrentDictionary<IPEndPoint, Peer>();
        
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 6881), new Peer(null, IPAddress.Parse("192.168.1.10"), 6881));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("203.0.113.45"), 51413), new Peer (null, IPAddress.Parse("203.0.113.45"), 51413));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("198.51.100.23"), 49152), new Peer(null, IPAddress.Parse("198.51.100.23"), 49152));
        
        var expected = new UdpTrackerAnnounceResponse(
            interval: 12,
            transactionId: myTransactionId,
            peers: expectedPeers,
            failureReason: null
        );

        // 1 is the Action
        var input = new byte[] {0,0,0,1 }
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(myTransactionId)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(expected.Interval)))
            .Concat(new byte[]{0,0,0,0,0,0,0,0})
            .Concat(expected.Peers.SelectMany(peer => Peer.SerializePeer(peer.Value)))
            .ToArray();
        var actual = new UdpTrackerAnnounceResponse(expected.TransactionId, input);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UdpTrackerAnnounceResponse_TestConstructorWithResponseBytesLessThanTwenty_ReturnAnnounceResponseWithFailureReason()
    {
        const int interval = 11;
        var intervalBytes = BitConverter.GetBytes(interval);
        var actual = new UdpTrackerAnnounceResponse(11, intervalBytes);
        actual.FailureReason.Should().Be("the received response was too small, required 20 bytes but it was " + intervalBytes.Length);
    }

    [Fact]
    public void UdpTrackerAnnounceResponse_TestConstructorWithResponseWithInvalidPeers_ReturnAnnounceResponseWithFailureReason()
    {
        var myTransactionId = 32;
        // bytes should be at least 20 and for IP of the peers to be properly parsed they should also be a multiple of 6
        // the Peer class is going to return an exception because here the bytes are 22
        var actual = new UdpTrackerAnnounceResponse(myTransactionId,
            [0,0,0,1,0,0,0, 32, 33, 11, 22, 33, 11, 22, 33, 11, 22, 33, 19, 20, 21, 22]);

        actual.FailureReason.Should()
            .Be("there has been an error trying to evaluate peers in the response: Invalid tracker: peers length was not correct");
    }
}