namespace SharpTorrent.Tracker;

public class TrackerRequest(
    byte[] infoHash,
    string peerId,
    ushort port,
    ulong uploaded,
    ulong downloaded,
    ulong left,
    ushort @event)
{
    public byte[] InfoHash { get; private set; } = infoHash;
    public string PeerId = peerId;
    public uint Port { get; private set; } = port;
    public ulong Uploaded { get; private set; } = uploaded;
    public ulong Downloaded { get; private set; } = downloaded;
    public ulong Left = left;
    public uint Event { get; private set; } = @event;
}