using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SharpTorrent.Bencode;
using SharpTorrent.P2P;
using SharpTorrent.Tracker;
using SharpTorrent.Tracker.Http;
using SharpTorrent.Tracker.Udp;
using SharpTorrent.Utils;

namespace SharpTorrent.Torrent;

public class TorrentMetadata
{
    public readonly string Announce;
    public readonly TorrentInfo Info;
    private readonly List<string> _announceList = [];
    private readonly byte[] _infoHash;
    private readonly string _peerId;
    private readonly ulong _torrentLength;
    
    public TorrentMetadata(byte[] bencode)
    {
        var bencodeParser = new BencodeParser();
        var parsedDict =  bencodeParser.ParseBencode(bencode) as Dictionary<string, object> 
                         ?? throw new FormatException("Invalid torrent: parsed bencode is not a dict");
        
        // announce
        if (parsedDict.TryGetValue("announce", out var announce) && announce is string value) Announce = value;
        else throw new FormatException("Invalid torrent: announce field is missing or is not of the expected type");
        
        // infoMeta 
        if (parsedDict.TryGetValue("info", out var info) && info is Dictionary<string, object> infoDict)
            Info = new TorrentInfo(infoDict);
        else throw new FormatException("Invalid torrent: info dictionary is missing or is not of the expected type");
        
        // announce list
        if (parsedDict.TryGetValue("announce-list", out var announceList))
        {
            foreach (var innerListObject in (List<object>) announceList)
            {
                var innerList = (List<object>)innerListObject;
                // CHECK IF IS NOT A PRIVATE TRACKER
                foreach (var ann in innerList.Cast<string>().Where(ann => !ann.Contains("?passkey=")))
                {
                    _announceList.Add(ann);
                }
            }
        }

        _infoHash = BuildInfoHash();
        _peerId = GeneratePeerId();
        
        _torrentLength = Info.Length ?? Info.Files!.Select(file => file.Length).Aggregate(0UL, (acc, val) => acc + val);
    }

    public TorrentMetadata(string pathToTorrent) : this(File.ReadAllBytes(pathToTorrent)) {}


    private async Task<ConcurrentDictionary<IPEndPoint, Peer>> GetPeers(int maxConns)
    {
        _announceList.Insert(0, Announce);
        const int port = 6881;
        
        var key = (uint) RandomNumberGenerator.GetInt32(1,int.MaxValue);
        var udpRequest = new UdpTrackerConnectionRequest(_infoHash, _peerId, left: _torrentLength, maxConns, key);
        var httpRequest = new HttpTrackerRequest(_infoHash, _peerId, port, 0, 0, left: _torrentLength, 0);
        var trackerManager = new TrackerManager(httpRequest, udpRequest);
        return await trackerManager.AggregatePeersFromTrackers(maxConns, _announceList);
    }
    
    private byte[] BuildInfoHash()
    {
        BencodeEncoder bencodeEncoder = new();
        var bencodeBytes = bencodeEncoder.EncodeToBencode(Info.InfoDict);
        return SHA1.HashData(bencodeBytes);
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

    private byte[][] SplitPieces()
    {
        var length = Info.Pieces.Length / 20;
        var piecesBuff = new byte[length][];
        for (var i = 0; i < length; i++)
        {
            piecesBuff[i] = Info.Pieces
                .Take(i..(i + 20))
                .ToArray();
        }

        return piecesBuff;
    }
    
    public async Task Download(int maxConns)
    {
        var peers = await GetPeers(maxConns);
        Singleton.Logger.LogInformation("Finished retrieving peers, found {Found} peers", peers.Count);
        if (peers.IsEmpty) return;

        var splitPieces = SplitPieces();


        var peerManager = new PeerManager(peers, splitPieces, _infoHash, _peerId, _torrentLength, Info.PieceLength);
        await peerManager.Download();
    }
}