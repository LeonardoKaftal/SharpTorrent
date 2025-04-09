using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P;
using SharpTorrent.Utils;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker.Udp;

public class UdpTrackerAnnounceResponse
{
    public const int Action = 1; // Announce
    public readonly int TransactionId;
    public uint Interval { get; private set; }
    public ConcurrentDictionary<IPEndPoint, Peer> Peers { get; private set; }
    public string? FailureReason { get; private set; }


    public UdpTrackerAnnounceResponse(int myTransactionId, byte[] rawAnnounceResponse)
    {
        TransactionId = myTransactionId;
        ParseAnnounceResponse(rawAnnounceResponse);
    }

    public UdpTrackerAnnounceResponse(int transactionId, uint interval, ConcurrentDictionary<IPEndPoint, Peer> peers, string? failureReason)
    {
        TransactionId = transactionId;
        Interval = interval;
        Peers = peers;
        FailureReason = failureReason;
    }


    private void ParseAnnounceResponse(byte[] rawAnnounceResponse)
    {
        if (rawAnnounceResponse.Length < 20)
        {
            FailureReason = "the received response was too small, required 20 bytes but it was " + rawAnnounceResponse.Length;
            return;
        }

        var action = BigEndianToInt32(rawAnnounceResponse[0..4]);
        if (action != Action)
        {
            FailureReason = $"action field was not set to {Action} but was instead {action}";
        }
        
        var transactionId = BigEndianToInt32(rawAnnounceResponse[4..8]);
        if (TransactionId != transactionId)
        {
            FailureReason = "transaction id is different from the one that has been received";
            return;
        }

        Interval = (uint) BigEndianToInt32(rawAnnounceResponse[8..12]);
        
        // skipping seeders and leechers

        var peersBytes = rawAnnounceResponse[20..];
        
        try
        {
            Peers = Peer.GetPeersFromCompactResponse(peersBytes);
        }
        catch (Exception e)
        {
            Peers = [];
            FailureReason = "there has been an error trying to evaluate peers in the response: " + e.Message;
        }
    }
    
}