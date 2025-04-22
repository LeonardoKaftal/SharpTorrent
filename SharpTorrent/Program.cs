using Microsoft.Extensions.Logging;
using SharpTorrent.Torrent;
using SharpTorrent.Utils;

if (args.Length < 1)
{
    Singleton.Logger.LogError("USAGE: SharpTorrent [TORRENT-PATH] optional:[MAX-NUMBER-OF-CONNECTION]");
    return;
}
foreach (var line in File.ReadLines("Banner.txt"))
{
    Console.WriteLine(line);
}

var torrent = new TorrentMetadata(args[0]);
// default value
var maxConns = 500;

if (args.Length > 1)
{
    if (int.TryParse(args[1], out var num)) maxConns = num;
    else Singleton.Logger.LogError("ERROR: impossible to parse maxConns parameter, USING DEFAULT VALUE");
}

await torrent.Download(maxConns);