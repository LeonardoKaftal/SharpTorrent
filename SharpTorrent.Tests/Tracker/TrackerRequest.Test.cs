using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.Torrent;
using SharpTorrent.Tracker;
using Xunit;

namespace SharpTorrent.Tests.Tracker;

[TestSubject(typeof(SharpTorrent.Tracker.TrackerRequest))]
public class TrackerRequestTest
{
    [Fact]
    public void TrackerRequest_TestConstructor_ReturnTrackerRequest()
    {
        var expected = new TrackerRequest(
            infoHash: [216, 247, 57, 206, 195, 40, 149, 108, 204, 91, 191, 31, 134, 217, 253, 207, 219, 168, 206, 182],
            peerId: string.Empty,
            port: 6881,
            uploaded: 0,
            downloaded: 0,
            left: 0,
            0
        );

        const string bencode = "d8:announce41:http://bttracker.debian.org:6969/announce4:infod6:lengthi351272960e4:name31:debian-10.2.0-amd64-netinst.iso12:piece lengthi262144e6:pieces40:1234567890abcdefghijabcdefghij1234567890ee";
        var torrentData = new TorrentMetadata(Encoding.UTF8.GetBytes(bencode));
        expected.PeerId = torrentData.TorrentTrackerRequestToSend.PeerId;
        expected.Left = torrentData.TorrentTrackerRequestToSend.Left;
        expected.Should().BeEquivalentTo(torrentData.TorrentTrackerRequestToSend);
    }
}