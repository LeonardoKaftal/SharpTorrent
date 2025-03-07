using System.Text;
using SharpTorrent.Torrent;

if (args.Length < 1)
{
    Console.WriteLine("USAGE: SharpTorrent [TORRENT-PATH]");
    return;
}
foreach (var line in File.ReadLines("Banner.txt"))
{
    Console.WriteLine(line);
    Thread.Sleep(75);
}

var torrentBencode = new TorrentBencode();
var bencodeToParse = File.ReadAllBytes("arch.iso.torrent"); 
var bencode = (Dictionary<string, object>) torrentBencode.ParseBencode(bencodeToParse);
Console.WriteLine(bencode);
