using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using SharpTorrent.P2P;
using SharpTorrent.Tracker.Http;
using SharpTorrent.Tracker.Udp;
using SharpTorrent.Utils;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker;

public class TrackerManager(HttpTrackerRequest httpTrackerRequest, UdpTrackerConnectionRequest udpTrackerConnectionRequest)
{
    private readonly ConcurrentBag<string> _failedTracker = [];
    
    public async Task<ConcurrentDictionary<IPEndPoint, Peer>> AggregatePeersFromTrackers(int maxConns, List<string> announceList)
    {
        Singleton.Logger.LogInformation("TRYING TO GET PEERS");
        var peerDict = await GetPeersFromTrackers(announceList, maxConns);

        if (peerDict.Count < maxConns)
        {
            Singleton.Logger.LogWarning("Not enough peers have been found from trackers, trying to get more from previous tracker that failed the peers transmission");
            var secondGroup = await GetPeersFromTrackers(_failedTracker.ToList(), maxConns);
            peerDict = MergePeersDictionary(peerDict, secondGroup, maxConns);
        }

        if (peerDict.IsEmpty) Singleton.Logger.LogCritical("CRITICAL ERROR, NO PEER HAS BEEN FOUND FOR DOWNLOADING THE TORRENT, ABORTING DOWNLOAD");
        return peerDict;
    }

    private async Task<ConcurrentDictionary<IPEndPoint, Peer>> GetPeersFromTrackers(List<string> announceList, int maxConns)
    {
        var peerDict = new ConcurrentDictionary<IPEndPoint, Peer>();
        var tasks = announceList.Select(ContactTracker).ToList();
        var responses = await Task.WhenAll(tasks);
        var i = 0;
        while (peerDict.Count < maxConns && i < responses.Length)
        {
            var response = responses[i];
            if (response.FailureReason != null)
            {
                Singleton.Logger.LogWarning("CLOSING CONNECTION WITH A TRACKER: {FailureReason}", response.FailureReason);
                _failedTracker.Add(response.Announce);
            }
            else peerDict = MergePeersDictionary(peerDict, response.Peers, maxConns);
            i++;
        }

        return peerDict;
    }

    private async Task<TrackerResponse> ContactTracker(string announce)
    {
        // invalid uri error
        if (!Uri.TryCreate(announce, UriKind.Absolute, out var uri)) return new TrackerResponse(0, [], FailureReason: $"Invalid tracker: {announce} is an invalid URI", announce);

        var uriScheme = uri.Scheme;
        switch (uriScheme)
        {
            case "http" or "https":
            {
                Singleton.Logger.LogInformation("Trying to get peers from HTTP tracker {Announce}", announce);
                var httpTrackerResponse = await httpTrackerRequest.SendRequestAsync(announce);
                return new TrackerResponse(httpTrackerResponse.Interval, httpTrackerResponse.Peers, httpTrackerResponse.FailureReason, announce);
            }
            case "udp":
            {
                Singleton.Logger.LogInformation("Trying to get peers from UDP tracker {Announce}", announce);
                var udpResponse = await udpTrackerConnectionRequest.SendAsync(announce);
                return new TrackerResponse(udpResponse.Interval, udpResponse.Peers, FailureReason: udpResponse.FailureReason, announce);
            }
            default:
                return new TrackerResponse(0, [], FailureReason: $"$tracker {announce} is not supported because {uriScheme} protocol is not supported", announce);
        }
    }
    
    // global mapper for tracker responses for both http and udp trackers
    private record TrackerResponse(
        ulong Interval,
        ConcurrentDictionary<IPEndPoint, Peer> Peers,
        string? FailureReason,
        string Announce);
}