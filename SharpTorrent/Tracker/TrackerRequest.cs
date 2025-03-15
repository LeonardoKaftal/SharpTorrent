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
    public readonly byte[] InfoHash = infoHash;
    public string PeerId = peerId;
    public readonly uint Port = port;
    public readonly ulong Uploaded = uploaded;
    public readonly ulong Downloaded = downloaded;
    public ulong Left = left;
    public readonly uint Event = @event;
}