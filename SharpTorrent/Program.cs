using Microsoft.Extensions.Logging;
using SharpTorrent;
using SharpTorrent.Torrent;

if (args.Length < 1)
{
    Singleton.Logger.LogError("USAGE: SharpTorrent [TORRENT-PATH] optional:[MAX-NUMBER-OF-CONNECTION]");
    return;
}
foreach (var line in File.ReadLines("Banner.txt"))
{
    Console.WriteLine(line);
}

try
{
    var torrent = new TorrentMetadata(args[0]);
    var maxConns = int.MaxValue;
    
    if (args.Length > 1)
    {
        if (int.TryParse(args[1], out var num)) maxConns = num;
        else Singleton.Logger.LogError("ERROR: impossible to parse maxConns parameter, USING ALL OF SEEDERS AVAILABLE");
    }

    var peers = await torrent.GetPeersFromTrackers(maxConns);
}
catch (Exception e)
{
    Singleton.Logger.LogCritical("CRITICAL ERROR: {}", e);
}