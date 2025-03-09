using System.Numerics;

namespace SharpTorrent.Torrent;

public class TorrentFile(BigInteger length, string path)
{
    public BigInteger Length = length;
    public string Path = path;
}