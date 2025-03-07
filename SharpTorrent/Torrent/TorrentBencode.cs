using SharpTorrent.Bencode;

namespace SharpTorrent.Torrent;

public class TorrentBencode
{
    public object ParseBencode(byte[] bencode)
    {
        BencodeParser bencodeParser = new();
        var result = bencodeParser.ParseBencode(bencode);
        return result;
    }
}