using System.Net;

namespace SharpTorrent.TorrentPeer;

public class Peer(string peerId, IPAddress ip, uint port)
{
    public readonly string PeerId = peerId;
    public readonly IPAddress Ip = ip;
    public readonly uint Port = port;
}