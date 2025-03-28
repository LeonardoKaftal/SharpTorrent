using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SharpTorrent.Bencode;
using SharpTorrent.TorrentPeer;

namespace SharpTorrent.Tracker;

public class TrackerResponse
{
    public readonly ulong Interval;
    public readonly HashSet<Peer> Peers;
    public readonly string? FailureReason;

    public TrackerResponse(byte[] bencode)
    {
        var bencodeParser = new BencodeParser();
        var parsedBencode = bencodeParser.ParseBencode(bencode);
        
        if (parsedBencode is not Dictionary<string, object> responseDict)
            throw new FormatException("Invalid tracker response: expected a dictionary as " +
                                      "tracker response but got " + parsedBencode.GetType());
        // error
        if (responseDict.TryGetValue("error", out var error))
        {
            FailureReason = "Invalid tracker: tracker throw an error : " + error;
            return;
        }
        try
        {
            Interval =  (ulong)(long)responseDict["interval"];
            var peerList = responseDict["peers"];
            // BEP 23
            if (peerList is byte[] bytes) Peers = GetPeersFromCompactResponse(bytes);
            else Peers = GetPeersFromNotCompactResponse((List<object>) peerList);
        }
        catch (KeyNotFoundException ex)
        {
            throw new FormatException("Invalid tracker: a mandatory field has not been found " +
                                      " in the tracker response: " + ex);
        }
        catch (InvalidCastException ex)
        {
            throw new FormatException("Invalid tracker: a mandatory field was of the wrong type " +
                                      "in the tracker response: " + ex);
        }
    }

    public TrackerResponse(ulong interval, HashSet<Peer> peers, string? error)
    {
        Interval = interval;
        Peers = peers;
        FailureReason = error;
    }

    private HashSet<Peer> GetPeersFromNotCompactResponse(List<object> peerList)
    {
        HashSet<Peer> toReturn = [];
        foreach (var peerObj in peerList)
        {
            if (peerObj is Dictionary<string, object> peerDict)
            {
                if (peerDict.TryGetValue("peer id", out var peerId) && peerId is not string)
                    throw new FormatException("Invalid tracker: received malformed peer," +
                                              " expcted a string for peerId field but got: " + peerObj.GetType());
                if (peerDict.TryGetValue("ip", out var ip) && peerId is not string) throw new FormatException("Invalid tracker: received malformed peer," +
                                              " expcted a string for ip field but got: " + peerObj.GetType());
                if (peerDict.TryGetValue("port", out var port) && port is not long)
                    throw new FormatException("Invalid tracker: received malformed peer," +
                                              " expcted a string for ip field but got: " + peerObj.GetType());

                if (port == null) throw new FormatException("Invalid tracker: received malformed peer, port was null");
                if (ip == null) throw new FormatException("Invalid tracker: received malformed peer, IP was null");


                toReturn.Add(
                    new Peer(
                        peerId: peerId as string,
                        ip: IPAddress.Parse((string)ip),
                        port: Convert.ToUInt16(port)
                    )
                );
            }
            else throw new FormatException("Invalid tracker: received malformed peers list, expected a dictionary for each peer but got: " + peerObj.GetType());
        }
        return toReturn;
    }
    
    
    private HashSet<Peer> GetPeersFromCompactResponse(byte[] peers)
    {
        HashSet<Peer> peersList = [];
        const int peerSize = 6;

        if (peers.Length % peerSize != 0)
            throw new FormatException("Invalid tracker: peers length was not correct");

        var peersNums = peers.Length / peerSize;

        for (var i = 0; i < peersNums; i++)
        {
            var startingOffset = i * peerSize;
            var ip = new IPAddress(peers[startingOffset..(startingOffset + 4)]);
            var port = (ushort)((peers[startingOffset + 4] << 8) | peers[startingOffset + 5]);
            peersList.Add(new Peer(null, ip, port));
        }
        return peersList;
    }
}