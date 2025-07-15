namespace SharpTorrent.Torrent;

using System.IO;

public class TorrentFile
{
    public readonly ulong Length;
    public readonly string FilePath;
    public readonly string FileName;

    public TorrentFile(ulong length, string filePath)
    {
        this.Length = length;
        this.FilePath = filePath;
        this.FileName = GetFileName();
    }

    private string GetFileName()
    {
        return Path.GetFileName(FilePath);
    }
};
