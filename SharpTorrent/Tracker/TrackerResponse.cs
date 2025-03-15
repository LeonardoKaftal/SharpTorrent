using System.Net;
using SharpTorrent.Bencode;
using SharpTorrent.TorrentPeer;

namespace SharpTorrent.Tracker;

public class TrackerResponse
{
    public readonly ulong Interval;
    public readonly List<Peer> Peers;

    public TrackerResponse(byte[] bencode)
    {
        var bencodeParser = new BencodeParser();
        var parsedBencode = bencodeParser.ParseBencode(bencode);
        
        if (parsedBencode is not Dictionary<string, object> responseDict)
            throw new FormatException("Invalid tracker response: expected a dictionary as " +
                                      "tracker response but got " + parsedBencode.GetType());
        // error
        if (responseDict.TryGetValue("error", out var error)) throw new HttpRequestException(error as string);
        try
        {

            Interval =  (ulong)(long)responseDict["interval"];
            var peerDict = (List<object>) responseDict["peers"];
            Peers = GetPeers(peerDict);
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
    public TrackerResponse(ulong interval, List<Peer> peers)
    {
        Interval = interval;
        Peers = peers;
    }

    private List<Peer> GetPeers(List<object> peerList)
    {
        List<Peer> toReturn = [];
        foreach (var peerObj in peerList)
        {
            if (peerObj is Dictionary<string, object> peerDict)
            {
                if (peerDict.TryGetValue("peer id", out var peerId) && peerId is not string)
                        throw new FormatException("Invalid tracker: received malformed peer," + 
                                                  " expcted a string for peerId field but got: " + peerObj.GetType());
                if (peerDict.TryGetValue("ip", out var ip) && peerId is not string)
                        throw new FormatException("Invalid tracker: received malformed peer," +
                                            " expcted a string for ip field but got: " + peerObj.GetType());
                if (peerDict.TryGetValue("port", out var port) && port is not long)
                    throw new FormatException("Invalid tracker: received malformed peer," +
                                              " expcted a string for ip field but got: " + peerObj.GetType());

                toReturn.Add(
                    new Peer(
                        peerId: peerId as string,
                        ip: IPAddress.Parse(ip as string),
                        port: Convert.ToUInt32(port)
                    )
                );
            }
            
            else throw new FormatException("Invalid tracker: received malformed peer," +
                                           " expcted a dictionary but got: " + peerObj.GetType());
        }
        
        return toReturn;
    }
}