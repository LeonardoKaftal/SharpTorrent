namespace SharpTorrent.Torrent;

using System.IO;

public record TorrentFile(ulong Length, string FilePath)
{
    public string FileName { get; } = Path.GetFileName(FilePath);
};
