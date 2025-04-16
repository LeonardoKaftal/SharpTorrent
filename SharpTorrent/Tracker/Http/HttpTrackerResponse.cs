using System.Collections.Concurrent;
using System.Net;
using SharpTorrent.Bencode;
using SharpTorrent.P2P;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker.Http;

public class HttpTrackerResponse
{
    public readonly ulong Interval;
    public ConcurrentDictionary<IPEndPoint, Peer> Peers { get; private set; }
    public readonly string? FailureReason;
    public readonly string Announce;
    
    // no exception need to be thrown as the program should not end for a tracker error
    public HttpTrackerResponse(byte[] bencode, string announce)
    {
        try
        {
            Announce = announce;
            var bencodeParser = new BencodeParser();
            var parsedBencode = bencodeParser.ParseBencode(bencode);

            if (parsedBencode is not Dictionary<string, object> responseDict)
            {
               FailureReason = $"Invalid tracker: invalid response from {announce}: expected a dictionary as " +
                                          "tracker response but got " + parsedBencode.GetType();
                return;
            }
            if (responseDict.TryGetValue("error", out var error))
            {
                FailureReason = $"Invalid tracker: tracker {announce} thrown an http error : " + error;
                return;
            }
            Interval = (ulong)(long) responseDict["interval"];
            GetPeersFromHttpTracker(responseDict); 
        }
        catch (KeyNotFoundException ex)
        {
            FailureReason = "Mandatory field has not been found " +
                            " in the tracker response: " + ex.Message;
        }
        catch (InvalidCastException ex)
        {
            FailureReason = "Mandatory field was of the wrong type " +
                            "in the tracker response: " + ex.Message;
        }
        catch (FormatException ex)
        {
            FailureReason = "there has been an error parsing the responses as it was malformed: " + ex.Message;
        }
    }

    public HttpTrackerResponse(ulong interval, ConcurrentDictionary<IPEndPoint, Peer> peers,  string announce)
    {
        Interval = interval;
        Peers = peers;
        Announce = announce;
    }

    public HttpTrackerResponse(string failureReason)
    {
        FailureReason = failureReason;
        Peers = [];
        Announce = string.Empty;
    }

    private void GetPeersFromHttpTracker(Dictionary<string, object> responseDict)
    {
        var peerList = responseDict["peers"];
        if (peerList is byte[] bytes) Peers = Peer.GetPeersFromCompactResponse(bytes);
        else Peers = Peer.GetPeersFromNotCompactResponse((List<object>)peerList);
        if (!responseDict.TryGetValue("peers6", out var peers6)) return;
        // BEP 23 with Ipv6
        var result = Peer.GetPeers6FromCompactResponse((byte[])peers6);
        Peers = MergePeersDictionary(Peers, result, int.MaxValue);
    }
}