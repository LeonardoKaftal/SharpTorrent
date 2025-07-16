using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using SharpTorrent.Bencode;
using SharpTorrent.Torrent;
using Xunit;

namespace SharpTorrent.Tests.Torrent;

[TestSubject(typeof(SharpTorrent.Torrent.TorrentMetadata))]
public class TorrentMetadataTest
{

    [Fact]
    public void TorrentMetadata_TestConstructor_ReturnTorrent()
    {
        // given
        var bencodeEncoder = new BencodeEncoder();
        var expected = new Dictionary<string, object>
        {
            ["announce"] = "http://bttracker.debian.org:6969/announce",
            ["info"] = new Dictionary<string, object>
            {
                ["pieces"] = "1234567890abcdefghijabcdefghij1234567890"u8.ToArray(),
                ["piece length"] = (uint) 262144,
                ["length"] = (ulong) 351272960,
                ["name"] = "debian-10.2.0-amd64-netinst.iso"
            }
        };
        
        // when
        
        // produce and test a valid bencode for the dictionary
        var inputBencode = bencodeEncoder.EncodeToBencode(expected);
        
        // test TorrentMetadata constructor to see if it produce the right value
        var actual = new TorrentMetadata(inputBencode);
        
        // then
        actual.Announce.Should().Be(expected["announce"] as string);
        var infoDict = expected["info"] as Dictionary<string, object>;
        actual.Info.Length.Should().Be((ulong)infoDict["length"]);
        actual.Info.Name.Should().Be(infoDict["name"] as string);
        actual.Info.Pieces.Should().BeEquivalentTo((byte[])infoDict["pieces"]);
        actual.Info.PieceLength.Should().Be((uint)infoDict["piece length"]);
    }

    [Fact]
    public void TorrentMetadata_TestConstructorWithNoAnnounce_ThrowFormatException()
    {
        // given
        const string bencode = "d4:infod6:lengthi351272960e4:name31:debian-10.2.0-amd64-netinst.iso12:piece lengthi262144e6:pieces40:1234567890abcdefghijabcdefghij1234567890ee";
        // when 
        var actual = () => new TorrentMetadata(Encoding.UTF8.GetBytes(bencode));
        // then
        actual.Should().Throw<FormatException>().WithMessage("Invalid torrent: *");
        
    }
    
    [Fact]
    public void TorrentMetadata_TestConstructorWithNoFilesOrLength_ThrowFormatException()
    {
        // given
        const string bencode = "d8:announce41:http://bttracker.debian.org:6969/announce4:infod4:name31:debian-10.2.0-amd64-netinst.iso12:piece lengthi262144e6:pieces40:1234567890abcdefghijabcdefghij1234567890ee";
        // when
        var actual = () => new TorrentMetadata(Encoding.UTF8.GetBytes(bencode));
        // then
        actual.Should().Throw<FormatException>().WithMessage("Invalid torrent: *");
    }
}