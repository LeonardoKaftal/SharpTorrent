using System.Collections.Concurrent;
using System.Net;
using SharpTorrent.Bencode;
using SharpTorrent.P2P;

namespace SharpTorrent.Tracker.Http;

public class HttpTrackerResponse
{
    public readonly ulong Interval;
    public readonly ConcurrentDictionary<IPEndPoint, Peer> Peers;
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
            var peerList = responseDict["peers"];
            if (peerList is byte[] bytes) Peers = Peer.GetPeersFromCompactResponse(bytes);
            else Peers = Peer.GetPeersFromNotCompactResponse((List<object>)peerList);
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

    public HttpTrackerResponse(ulong interval, ConcurrentDictionary<IPEndPoint, Peer> peers, string? failureReason, string announce)
    {
        Interval = interval;
        Peers = peers;
        FailureReason = failureReason;
        Announce = announce;
    }

}