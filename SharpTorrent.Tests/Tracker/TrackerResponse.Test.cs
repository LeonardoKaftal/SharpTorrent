using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.TorrentPeer;
using SharpTorrent.Tracker;
using Xunit;

namespace SharpTorrent.Tests.Tracker;

[TestSubject(typeof(SharpTorrent.Tracker.TrackerResponse))]
public class TrackerResponseTest
{
    [Fact]
    public void TrackerResponse_TestConstructor_ReturnTrackerResponse()
    {
        const string responseBencode =
            "d8:intervali1800e5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eed2:ip12:203.0.113.457:peer id20:-AZ2060-7vfG3wF6wXMz4:porti51413eed2:ip13:198.51.100.237:peer id20:-UT2210-KlfG9oP5wYZQ4:porti49152eeee";
        
        var expected = new TrackerResponse(
            interval: 1800,
            peers: [
                new Peer("-TR2940-6wfG2wk6wWLc", IPAddress.Parse("192.168.1.10"), 6881),
                new Peer("-AZ2060-7vfG3wF6wXMz", IPAddress.Parse("203.0.113.45"), 51413),
                new Peer("-UT2210-KlfG9oP5wYZQ", IPAddress.Parse("198.51.100.23"), 49152)
            ],
            null
        );

        var actual = new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        actual.Should().BeEquivalentTo(expected);
    }
    
    
    [Fact]
    public void TrackerResponse_TestConstructorWithCompactResponse_ReturnTrackerResponse()
    {
        // BEP 23 the peers are a string
        byte[] responseBencodeBytes =
        [
            (byte)'d', (byte)'8', (byte)':', (byte)'i', (byte)'n', (byte)'t', (byte)'e', (byte)'r', (byte)'v', (byte)'a', (byte)'l',
            (byte)'i', (byte)'1', (byte)'8', (byte)'0', (byte)'0', (byte)'e', (byte)'5', (byte)':', (byte)'p', (byte)'e', (byte)'e', (byte)'r', (byte)'s',
            (byte)'1', (byte) '8', (byte) ':',
            0xC0, 0xA8, 0x01, 0x0A, 0x1A, 0xE1, 0xCB, 0x00, 0x71, 0x2D, 0x1A, 0xE2, 0xC6, 0x33, 0x64, 0x17, 0x1A, 0xE3,
            (byte)'e'
        ];
        
        var expected = new TrackerResponse(
            interval: 1800,
            peers: [
                new Peer(null, IPAddress.Parse("192.168.1.10"), 6881),
                new Peer(null, IPAddress.Parse("203.0.113.45"), 6882),
                new Peer(null, IPAddress.Parse("198.51.100.23"), 6883)
            ],
            null
        );

        var actual = new TrackerResponse(responseBencodeBytes);
        actual.Should().BeEquivalentTo(expected);
    }
    
    
    [Fact]
    public void TrackerResponse_TestConstructorWithInvalidCompactResponse_ThrowFormatException()
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

        var actual = () => new TrackerResponse(responseBencodeBytes);
        actual.Should().Throw<FormatException>().WithMessage("Invalid tracker: peers length was not correct");
    }

    [Fact]
    public void TrackerResponse_TestConstructorWithNotADictionary_ThrowsFormatException()
    {
        const string responseBencode = "l8:intervali1800ee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker response: expected a dictionary as tracker response but got *");
    }

    [Fact]
    public void TrackerResponse_TestConstructorWithMissingIntervalField_ThrowsFormatException()
    {
        const string responseBencode = "d5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: a mandatory field has not been found*");
    }

    [Fact]
    public void TrackerResponse_TestConstructorMissingPeersField_ThrowsFormatException()
    {
        // Arrange - a response missing the peers field
        const string responseBencode = "d8:intervali1800ee";
        
        // Act & Assert
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: a mandatory field has not been found*");
    }

    [Fact]
    public void TrackerResponse_TestConstructorWithWrongIntervalType_ThrowsFormatException()
    {
        const string responseBencode = "d8:interval4:18005:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: a mandatory field was of the wrong type*");
    }


    [Fact]
    public void TrackerResponse_TestConstructorWithPeerNotAsDictionaryOrString_ThrowsFormatException()
    {
        const string responseBencode = "d8:intervali1800e5:peersli58eee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: *");
    }

    [Fact]
    public void TrackerResponse_TestConstructorWithPeerIdWrongType_ThrowsFormatException()
    {
        const string responseBencode = "d8:intervali1800e5:peersld2:ip12:192.168.1.107:peer idi12345e4:porti6881eeee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: *");
    }

    [Fact]
    public void TrackerResponse_TestConstructorWithPortWrongType_ThrowsFormatException()
    {
        const string responseBencode = "d8:intervali1800e5:peersld2:ip12:192.168.1.107:peer id20:-TR2940-6wfG2wk6wWLc4:port5:6881eeee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid tracker: *");
    }

    [Fact]
    public void TrackerResponse_InvalidIpAddress_ThrowsFormatException()
    {
        const string responseBencode = "d8:intervali1800e5:peersld2:ip10:999.999.1.7:peer id20:-TR2940-6wfG2wk6wWLc4:porti6881eeee";
        
        Action act = () => new TrackerResponse(Encoding.UTF8.GetBytes(responseBencode));
        act.Should().Throw<FormatException>()
           .WithMessage("*");  // FormatException from IPAddress.Parse
    }
}