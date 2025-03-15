using System.Net;

namespace SharpTorrent.TorrentPeer;

public class Peer(string peerId, IPAddress ip, uint port)
{
    public string PeerId { get; private set; } = peerId;
    public IPAddress Ip { get; private set; } = ip;
    public uint Port { get; private set; } = port;
}