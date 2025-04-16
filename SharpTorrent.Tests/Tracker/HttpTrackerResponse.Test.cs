using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.P2P;
using SharpTorrent.Tracker.Http;
using Xunit;

namespace SharpTorrent.Tests.Tracker;

[TestSubject(typeof(HttpTrackerResponse))]
public class HttpTrackerResponseTest
{
    [Fact]
    public void HttpTrackerResponse_TestConstructor_ReturnTrackerResponse()
    { 
        const string responseBencode =
            "d8:intervali1800e5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eed2:ip12:203.0.113.457:peer id20:-AZ2060-7vfG3wF6wXMz4:porti51413eed2:ip13:198.51.100.237:peer id20:-UT2210-KlfG9oP5wYZQ4:porti49152eeee";
        

        var expectedPeers = new ConcurrentDictionary<IPEndPoint, Peer>();
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("203.0.113.45"), 51413), new Peer("-AZ2060-7vfG3wF6wXMz", IPAddress.Parse("203.0.113.45"), 51413));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 6881), new Peer("-TR2940-6wfG2wk6wWLc", IPAddress.Parse("192.168.1.10"), 6881));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("198.51.100.23"), 49152), new Peer("-UT2210-KlfG9oP5wYZQ", IPAddress.Parse("198.51.100.23"), 49152));

        var expected = new HttpTrackerResponse(1800, expectedPeers, "");
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");

        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void HttpTrackerResponse_TestConstructorWithCompactResponse_ReturnTrackerResponse()
    {
        // BEP 23
        byte[] responseBencodeBytes =
        [
            (byte)'d', (byte)'8', (byte)':', (byte)'i', (byte)'n', (byte)'t', (byte)'e', (byte)'r', (byte)'v', (byte)'a', (byte)'l',
            (byte)'i', (byte)'1', (byte)'8', (byte)'0', (byte)'0', (byte)'e', (byte)'5', (byte)':', (byte)'p', (byte)'e', (byte)'e', (byte)'r', (byte)'s',
            (byte)'1', (byte)'8', (byte)':',
            0xC0, 0xA8, 0x01, 0x0A, 0x1A, 0xE1,  // 192.168.1.10:6881
            0xCB, 0x00, 0x71, 0x2D, 0x1A, 0xE2,  // 203.0.113.45:6882
            0xC6, 0x33, 0x64, 0x17, 0x1A, 0xE3,  // 198.51.100.23:6883
            (byte)'e'
        ];

        var expectedPeers = new ConcurrentDictionary<IPEndPoint, Peer>();
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 6881), new Peer(null, IPAddress.Parse("192.168.1.10"), 6881));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("203.0.113.45"), 6882), new Peer(null, IPAddress.Parse("203.0.113.45"), 6882));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("198.51.100.23"), 6883), new Peer(null, IPAddress.Parse("198.51.100.23"), 6883));

        var expected = new HttpTrackerResponse(1800, expectedPeers, "");
        var actual = new HttpTrackerResponse(responseBencodeBytes, "");

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorWithNonCompactResponseWithIpv6_returnTrackerResponse()
    {
        const string responseBencode =
            "d8:intervali1800e5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eed2:ip12:203.0.113.457:peer id20:-AZ2060-7vfG3wF6wXMz4:porti51413ee" +
            "d2:ip13:198.51.100.237:peer id20:-UT2210-KlfG9oP5wYZQ4:porti49152ee" +
            "d2:ip38:2001:db8:3333:4444:5555:6666:7777:88887:peer id20:-UT2210-KlfG9oP5wYZQ4:porti6882ee" +
            "d2:ip39:2001:0db8:85a3:0000:0000:8a2e:0370:73347:peer id20:-UT2210-KlfG9oP5wYZQ4:porti6883eeee";
        

        var expectedPeers = new ConcurrentDictionary<IPEndPoint, Peer>();
        // ipv 4
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("203.0.113.45"), 51413), new Peer("-AZ2060-7vfG3wF6wXMz", IPAddress.Parse("203.0.113.45"), 51413));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 6881), new Peer("-TR2940-6wfG2wk6wWLc", IPAddress.Parse("192.168.1.10"), 6881));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("198.51.100.23"), 49152), new Peer("-UT2210-KlfG9oP5wYZQ", IPAddress.Parse("198.51.100.23"), 49152));

        // ipv 6
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("2001:db8:3333:4444:5555:6666:7777:8888"), 6882), new Peer("-UT2210-KlfG9oP5wYZQ", IPAddress.Parse("2001:db8:3333:4444:5555:6666:7777:8888"), 6882));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 6883), new Peer("-UT2210-KlfG9oP5wYZQ", IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 6883));

        
        var expected = new HttpTrackerResponse(1800, expectedPeers, "");
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");

        actual.Should().BeEquivalentTo(expected);
    }
    
    
    [Fact]
    public void HttpTrackerResponse_TestConstructorWithCompactResponseWithIpv6_ReturnTrackerResponse()
    {
        // BEP 23
        byte[] responseBencodeBytes =
        [
            (byte)'d', (byte)'8', (byte)':', (byte)'i', (byte)'n', (byte)'t', (byte)'e', (byte)'r', (byte)'v', (byte)'a', (byte)'l',
            (byte)'i', (byte)'1', (byte)'8', (byte)'0', (byte)'0', (byte)'e', (byte)'5', (byte)':', (byte)'p', (byte)'e', (byte)'e', (byte)'r', (byte)'s',
            (byte)'1', (byte)'8', (byte)':',
            0xC0, 0xA8, 0x01, 0x0A, 0x1A, 0xE1,
            0xCB, 0x00, 0x71, 0x2D, 0x1A, 0xE2,
            0xC6, 0x33, 0x64, 0x17, 0x1A, 0xE3,
            (byte)'6', (byte)':', (byte)'p', (byte)'e', (byte)'e', (byte)'r', (byte)'s', (byte)'6',
            (byte)'3', (byte)'6', (byte) ':',
            // IPv6 #1
            0x20, 0x01, 0x0d, 0xb8, 0x33, 0x33, 0x44, 0x44,
            0x55, 0x55, 0x66, 0x66, 0x77, 0x77, 0x88, 0x88,
            0x1A, 0xE2,
            // IPv6 #2
            0x20, 0x01, 0x0d, 0xb8, 0x85, 0xa3, 0x00, 0x00,
            0x00, 0x00, 0x8a, 0x2e, 0x03, 0x70, 0x73, 0x34,
            0x1A, 0xE3,
            (byte) 'e'
        ];

        var expectedPeers = new ConcurrentDictionary<IPEndPoint, Peer>();
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 6881), new Peer(null, IPAddress.Parse("192.168.1.10"), 6881));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("203.0.113.45"), 6882), new Peer(null, IPAddress.Parse("203.0.113.45"), 6882));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("198.51.100.23"), 6883), new Peer(null, IPAddress.Parse("198.51.100.23"), 6883));

        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("2001:db8:3333:4444:5555:6666:7777:8888"), 6882), new Peer(null, IPAddress.Parse("2001:db8:3333:4444:5555:6666:7777:8888"), 6882));
        expectedPeers.TryAdd(new IPEndPoint(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 6883), new Peer(null, IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"), 6883));

        var expected = new HttpTrackerResponse(1800, expectedPeers, "");
        var actual = new HttpTrackerResponse(responseBencodeBytes, "");

        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void HttpTrackerResponse_TestConstructorWithInvalidCompactResponse_ReturnFailureReason()
    {
        // BEP 23 the peers are a string
        byte[] responseBencodeBytes =
        [
            (byte)'d', (byte)'8', (byte)':', (byte)'i', (byte)'n', (byte)'t', (byte)'e', (byte)'r', (byte)'v', (byte)'a', (byte)'l',
            (byte)'i', (byte)'1', (byte)'8', (byte)'0', (byte)'0', (byte)'e', (byte)'5', (byte)':', (byte)'p', (byte)'e', (byte)'e', (byte)'r', (byte)'s',
            (byte)'1', (byte) '7', (byte) ':',
            0xC0, 0xA8, 0x01, 0x0A, 0x1A, 0xE1, 0xCB, 0x00, 0x71, 0x2D, 0x1A, 0xE2, 0xC6, 0x33, 0x64, 0x17, 0x1A,
            (byte)'e'
        ];

        var actual = new HttpTrackerResponse(responseBencodeBytes, "");
        actual.FailureReason.Should().StartWith("there has been an error parsing the responses as it was malformed");
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorWithNotADictionary_ReturnFailureReason()
    {
        const string responseBencode = "l8:intervali1800ee";
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("Invalid tracker: invalid response from ");
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorWithMissingIntervalField_ReturnFailureReason()
    {
        const string responseBencode = "d5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("Mandatory field has not been found");
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorMissingPeersField_ReturnFailureReason()
    {
        const string responseBencode = "d8:intervali1800ee";
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("Mandatory field has not been found");
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorWithWrongIntervalType_ReturnFailureReason()
    {
        const string responseBencode = "d8:interval4:18005:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";

        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("Mandatory field was of the wrong type");
    }

    [Fact]
    public void HttpTrackerResponse_TestConstructorWithPeerNotAsDictionaryOrString_ReturnFailureReason()
    {
        const string responseBencode = "d8:intervali1800e5:peersli58eee";
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("there has been an error parsing the responses as it was malformed");
    }


    [Fact]
    public void HttpTrackerResponse_InvalidIpAddress_ReturnFailureReason()
    {
        const string responseBencode = "d8:intervali1800e5:peersld2:ip10:999.999.1.7:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";
        var actual = new HttpTrackerResponse(Encoding.UTF8.GetBytes(responseBencode), "");
        actual.FailureReason.Should().StartWith("there has been an error parsing the responses as it was malformed");
    }
}