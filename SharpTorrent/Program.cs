using System.Reflection;
using Microsoft.Extensions.Logging;
using SharpTorrent.Torrent;
using SharpTorrent.Utils;

await Main();
return;

async Task Main()
{
    if (args.Length < 2)
    {
        Singleton.Logger.LogError(
            "USAGE: SharpTorrent [TORRENT-PATH] [DOWNLOAD-PATH] optional:[MAX-NUMBER-OF-CONNECTION]");
        return;
    }

    var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var bannerPath = Path.Combine(exeDirectory!, "Banner.txt");

    if (File.Exists(bannerPath))
    {
        foreach (var line in File.ReadLines(bannerPath))
        {
            Console.WriteLine(line);
        }
    }
    else
    {
        Singleton.Logger.LogWarning($"Banner.txt not found at: {bannerPath}");
    }

    var torrentPath = args[0];
    var downloadPath = args[1];
    if (!IsValidPath(downloadPath)) throw new ArgumentException("download path provided by argument is malformed");
    var torrent = new TorrentMetadata(torrentPath, downloadPath);
    // default value
    var maxConns = 120;

    if (args.Length > 2)
    {
        if (int.TryParse(args[2], out var num)) maxConns = num;
        else Singleton.Logger.LogError("ERROR: impossible to parse maxConns parameter, USING DEFAULT VALUE");
    }

    var isDownloaded = await torrent.Download(maxConns);
    // try one last time
    if (!isDownloaded)
    {
        Singleton.Logger.LogWarning("Attempting to download the torrent a second time");
        torrent = new TorrentMetadata(torrentPath, downloadPath);
        await torrent.Download(maxConns);
    }
}

bool IsValidPath(string path)
{
    if (string.IsNullOrWhiteSpace(path)) return false;
    
    try
    {
        Path.GetFullPath(path);
        return path.IndexOfAny(Path.GetInvalidPathChars()) == -1;
    } catch
    {
        return false;
    }
}
