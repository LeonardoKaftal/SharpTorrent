using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using SharpTorrent.Utils;

namespace SharpTorrent.P2P;

public readonly record struct Peer(string? PeerId, IPAddress Ip, ushort Port)
{
    public static ConcurrentDictionary<IPEndPoint,Peer> GetPeersFromNotCompactResponse(List<object> peerList)
    {
        ConcurrentDictionary<IPEndPoint, Peer> toReturn = [];
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

                
                var parsedIp = IPAddress.Parse((string)ip);
                var parsedPort = Convert.ToUInt16(port);
                toReturn[new IPEndPoint(parsedIp, parsedPort)] = new Peer(PeerId: peerId as string, Ip: parsedIp,  Port: parsedPort);
            }
            else throw new FormatException("Invalid tracker: received malformed peers list, expected a dictionary for each peer but got: " + peerObj.GetType());
        }
        return toReturn;
    }
    
    public static ConcurrentDictionary<IPEndPoint,Peer> GetPeersFromCompactResponse(byte[] peers)
    {
        ConcurrentDictionary<IPEndPoint,Peer> peerDict = [];
        const int peerSize = 6;

        if (peers.Length % peerSize != 0)
            throw new FormatException("Invalid tracker: peers length was not correct for IPV4");

        var peersNums = peers.Length / peerSize;

        for (var i = 0; i < peersNums; i++)
        {
            var startingOffset = i * peerSize;
            var ip = new IPAddress(peers[startingOffset..(startingOffset + 4)]);
            // + 4 and + 5 is the offset for the two bytes the port
            var port = (ushort)((peers[startingOffset + 4] << 8) | peers[startingOffset + 5]);
            peerDict[new IPEndPoint(ip, port)] = new Peer(null, ip, port);
        }
        
        return peerDict;
    }

    public static ConcurrentDictionary<IPEndPoint, Peer> GetPeers6FromCompactResponse(byte[] peers)
    {
        ConcurrentDictionary<IPEndPoint, Peer> peerDict = [];
        const int peerSize = 18;

        if (peers.Length % peerSize != 0) throw new FormatException("Invalid tracker: peers length was not correct for IPV6");

        var peerNums = peers.Length / 18;

        for (var i = 0; i < peerNums; i++)
        {
            var startingOffset = i * peerSize;
            var ip = new IPAddress(peers[startingOffset..(startingOffset + 16)]);
            // + 16 and + 17 is the offset for the two bytes the port
            var port = (ushort) ((peers[startingOffset + 16] << 8) | peers[startingOffset + 17]);
            peerDict[new IPEndPoint(ip, port)] = new Peer(null, ip, port);
        }

        return peerDict;
    }

    public static byte[] SerializePeer(Peer peer)
    {
        var portBuff = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(portBuff, peer.Port);
        
        return peer.Ip.GetAddressBytes()
            .Concat(portBuff)
            .ToArray();
    }

    public void RemovePeer(ConcurrentDictionary<IPEndPoint,Peer> peers)
    {
        var endpoint = new IPEndPoint(Ip, Port);
        if (!peers.TryRemove(endpoint, out var removed))
            Singleton.Logger.LogCritical("Peer {Ip} that should have been removed has not been found in the list of peers", removed.Ip.ToString());
    }
}