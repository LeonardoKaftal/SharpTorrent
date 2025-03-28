using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SharpTorrent.Bencode;
using SharpTorrent.TorrentPeer;
using SharpTorrent.Tracker;

namespace SharpTorrent.Torrent;

public class TorrentMetadata
{
    public readonly string Announce;
    public readonly TorrentInfo Info;
    public readonly List<string> AnnounceList = [];
    public readonly TrackerRequest TorrentTrackerRequestToSend;

    public TorrentMetadata(byte[] bencode)
    {
        var parsedBencode = ParseBencode(bencode);
        // announce
        if (parsedBencode.TryGetValue("announce", out var announce) && announce is string value) Announce = value;
        else throw new FormatException("Invalid torrent: announce field is missing or is not of the expected type");
        // info 
        if (parsedBencode.TryGetValue("info", out var info) && info is Dictionary<string, object> infoDict)
            Info = new TorrentInfo(infoDict);
        else throw new FormatException("Invalid torrent: info dictionary is missing or is not of the expected type");
        // announce list
        if (parsedBencode.TryGetValue("announce-list", out var announceList))
        {
            foreach (var innerListObject in (List<object>) announceList)
            {
                var innerList = (List<object>)innerListObject;
                // CHECK IF IS NOT A PRIVATE TRACKER
                foreach (var ann in innerList.Cast<string>().Where(ann => !ann.Contains("?passkey=")))
                {
                    AnnounceList.Add(ann);
                }
            }
        }
        TorrentTrackerRequestToSend = BuildTrackerRequest();
    }

    public TorrentMetadata(string pathToTorrent) : this(File.ReadAllBytes(pathToTorrent)) {}
    
    public async Task<HashSet<Peer>> GetPeersFromTrackers(int maxConns)
    {
        Singleton.Logger.LogInformation("TRYING TO GET PEERS");
        if (maxConns == int.MaxValue) Singleton.Logger.LogWarning("DUE THE FACT THAT MAX CONNS HAS NOT BEEN SET CLIENT WILL TAKE ALL OF THE AVAILABLE PEERS," +
                                                          " THE DOWNLOAD WILL BE MUCH FASTER BUT THIS WILL CAUSE MUCH MORE SYSTEM RESOURCES TO BE USED BY THE CLIENT.");
        var peersSet = new HashSet<Peer>();
        var result = await GetResponseWithPeersFromTracker(Announce);
        if (result.FailureReason != null) Singleton.Logger.LogError(result.FailureReason);
        else peersSet.UnionWith(result.Peers);

        if (AnnounceList.Count > 0)
        {
            var tasks = AnnounceList.Select(GetResponseWithPeersFromTracker).ToList();
            var responses = await Task.WhenAll(tasks);
            foreach (var response in responses)
            {
                if (response.FailureReason != null) Singleton.Logger.LogError(response.FailureReason);
                else peersSet.UnionWith(response.Peers);
            }
        }

        if (peersSet.Count == 0) Singleton.Logger.LogCritical("ERROR, NO PEER HAS BEEN FOUND FOR DOWNLOADING THE TORRENT, TRYING AGAIN LATER");
        return maxConns == int.MaxValue ? peersSet : peersSet.Take(maxConns).ToHashSet();
    }
  
    private Dictionary<string,object> ParseBencode(byte[] bencode)
    {
        BencodeParser bencodeParser = new();
        return (Dictionary<string, object>) bencodeParser.ParseBencode(bencode);
    }
    
    private TrackerRequest BuildTrackerRequest()
    {
        const ushort port = 6881;
        ulong left = 0;
        if (Info.Length == null)
        {
            left = Info.Files!.Aggregate(left, (current, file) => current + file.Length);
        }
        else left = (ulong) Info.Length;
        return new TrackerRequest(
            BuildInfoHash(), GeneratePeerId(), port, 0, 0, left, 0
        );
    }

    private byte[] BuildInfoHash()
    {
        BencodeEncoder bencodeEncoder = new();
        var bencodeBytes = bencodeEncoder.EncodeToBencode(Info.InfoDict);
        var result = SHA1.HashData(bencodeBytes);
        return result;
    }
    
    private string GeneratePeerId()
    {
        // generate random string of length 20
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable
            .Repeat(chars, 20)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }
    
    private async Task<TrackerResponse> GetResponseWithPeersFromTracker(string announce)
    { 
        Singleton.Logger.LogInformation("Trying to get peers from tracker {Announce}", announce);

        try
        {
            return await TorrentTrackerRequestToSend.SendRequestAsync(announce);
        }
        catch (FormatException ex)
        {
            var error = $"Closing connection with tracker {announce} because of ERROR: {ex.Message}";
            return new TrackerResponse(0, [], error);
        }
        catch (Exception ex)
        {
            var error = $"Closing connection with tracker {announce} because of CRITICAL ERROR: unexpected error while trying to connect to the tracker: {ex.Message}";
            return new TrackerResponse(0, [], error);
        }
    }
}