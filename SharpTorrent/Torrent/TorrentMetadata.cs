using SharpTorrent.Bencode;

namespace SharpTorrent.Torrent;

public class TorrentMetadata
{
    public string Announce = string.Empty;
    public readonly TorrentInfo Info;

    // constructor used for testing
    public TorrentMetadata(byte[] bencode)
    {
        var parsedBencode = ParseBencode(bencode);
        var infoDict = (Dictionary<string, object>)(parsedBencode["info"]);
        Info = new TorrentInfo(infoDict);
    }

    public TorrentMetadata(string pathToTorrent) : this(File.ReadAllBytes(pathToTorrent)) {}
  
    private Dictionary<string,object> ParseBencode(byte[] bencode)
    {
        BencodeParser bencodeParser = new();
        var parsedBencode = (Dictionary<string, object>)bencodeParser.ParseBencode(bencode);
        if (parsedBencode.TryGetValue("announce", out var announce)) Announce = (string)(announce);
        else throw new FormatException("Invalid torrent: Torrent does not contain announce");
        return parsedBencode;
    }
}