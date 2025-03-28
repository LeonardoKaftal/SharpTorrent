using System.Net;

namespace SharpTorrent.TorrentPeer;

public class Peer(string? peerId, IPAddress ip, ushort port)
{
    public readonly string? PeerId = peerId;
    public readonly IPAddress Ip = ip;
    public readonly ushort Port = port;
}