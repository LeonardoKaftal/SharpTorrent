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

Console.WriteLine(args[0]);