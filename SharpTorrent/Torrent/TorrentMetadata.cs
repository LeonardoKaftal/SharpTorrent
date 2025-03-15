using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using SharpTorrent.Bencode;
using SharpTorrent.Tracker;

namespace SharpTorrent.Torrent;

public class TorrentMetadata
{
    public readonly string Announce = string.Empty;
    public readonly TorrentInfo Info;
    public readonly TrackerRequest TrackerRequest;

    // constructor used for testing
    public TorrentMetadata(byte[] bencode)
    {
        var parsedBencode = ParseBencode(bencode);
        // announce
        if (parsedBencode.TryGetValue("announce", out var announce) && announce is string value)
            Announce = value;
        else throw new FormatException("Invalid torrent: announce field is missing or is not of the expected type");
        // info 
        if (parsedBencode.TryGetValue("info", out var info) && info is Dictionary<string, object> infoDict)
            Info = new TorrentInfo(infoDict);
        else throw new FormatException("Invalid torrent: info dictionary is missing or is not of the expected type");
        TrackerRequest = BuildTrackerRequest();
    }

    public TorrentMetadata(string pathToTorrent) : this(File.ReadAllBytes(pathToTorrent)) {}
  
    private Dictionary<string,object> ParseBencode(byte[] bencode)
    {
        BencodeParser bencodeParser = new();
        return (Dictionary<string, object>) bencodeParser.ParseBencode(bencode);
    }
    
    private TrackerRequest BuildTrackerRequest()
    {
        const ushort port = 6881;
        ulong left = 0;
        if (Info.Length == null)
        {
            left = Info.Files!.Aggregate(left, (current, file) => current + file.Length);
        }
        else left = (ulong) Info.Length;
        return new TrackerRequest(
            BuildInfoHash(), GeneratePeerId(), port, 0, 0, left, 0
        );
    }

    private byte[] BuildInfoHash()
    {
        BencodeEncoder bencodeEncoder = new();
        var bencode = bencodeEncoder.EncodeToBencode(Info.InfoDict);
        var infoBytes = Encoding.UTF8.GetBytes(bencode);
        var result = SHA1.HashData(infoBytes);
        return result;
    }
    
    private string GeneratePeerId()
    {
        // generate random string of length 20
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable
            .Repeat(chars, 20)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }
}