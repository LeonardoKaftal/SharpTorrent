using System.Numerics;

namespace SharpTorrent.Torrent;

public class TorrentFile(ulong length, string path)
{
    public readonly ulong Length = length;
    public readonly string Path = path;
}