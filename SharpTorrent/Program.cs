﻿using SharpTorrent.Torrent;

if (args.Length < 1)
{
    Console.WriteLine("USAGE: SharpTorrent [TORRENT-PATH]");
    return;
}
foreach (var line in File.ReadLines("Banner.txt"))
{
    Console.WriteLine(line);
    Thread.Sleep(50);
}

var bencodeToParse = File.ReadAllBytes(args[0]); 
var torrent = new TorrentMetadata(bencodeToParse);