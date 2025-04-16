using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using SharpTorrent.P2P;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker.Udp;

public class UdpTrackerConnectionRequest(byte[] infoHash, string peerId, ulong left, int peersWanted, uint key)
{
    private long _connectionId = 0;
    
    public async Task<UdpTrackerAnnounceResponse> SendAsync(string announce)
    {
        var transactionId = (uint)RandomNumberGenerator.GetInt32(1,int.MaxValue);
        var connectionRequest = FormConnectionRequest(transactionId);
        var uri = new Uri(announce);
        var responses = new List<UdpTrackerAnnounceResponse>();
        
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);
            if (addresses.Length == 0) return new UdpTrackerAnnounceResponse($"Impossible to find ip for tracker {announce} trough DNS");
            foreach (var address in addresses)
            {
                var endPoint = new IPEndPoint(address, uri.Port);
                using var udpClient = endPoint.AddressFamily == AddressFamily.InterNetwork ? new UdpClient(AddressFamily.InterNetwork) : new UdpClient(AddressFamily.InterNetworkV6);
                
                // send connection request
                await udpClient.SendAsync(connectionRequest, endPoint);
                var receivedResponse = await UdpReceiveAsyncWithTimer(udpClient, 5) ?? await UdpReceiveAsyncWithTimer(udpClient, 10);
                
                if (receivedResponse == null) return new UdpTrackerAnnounceResponse($"Tracker {announce} did not respond in time to UDP connection request");
                
                if (!ConnectionResponseIsValid(receivedResponse?.Buffer!, transactionId)) 
                {
                    var error = $"Tracker {announce} have not sent a valid response to UDP connection request";
                    return new UdpTrackerAnnounceResponse(error);
                }

                responses.Add(await GetAnnounceResponse(udpClient, endPoint, announce));
                
            }
        }
        catch (Exception ex)
        {
            return new UdpTrackerAnnounceResponse($"Error sending connection request or trying to parse IP trough DNS of {announce}: {ex.Message}");
        }

        return MergeAllIpv4AndIpv6Peers(responses);
    }

    private static byte[] FormConnectionRequest(uint transactionId)
    {
        const long protocolId = 0x41727101980L;
        const int action = 0; // connect
        var buffer = ReverseIfLittleEndian(BitConverter.GetBytes(protocolId))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(action)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(transactionId)))
            .ToArray();
        return buffer;
    }

    private bool ConnectionResponseIsValid(byte[] buff, uint transactionId)
    {
        if (buff.Length != 16) return false;
        var action = BigEndianToInt32(buff[0..4]);
        if (action != 0) return false;
        var receivedTransactionId = BigEndianToInt32(buff[4..8]);
        if (receivedTransactionId != transactionId) return false;
        _connectionId = BigEndianToInt64(buff[8..16]);
        return true;
    }

    private async Task<UdpTrackerAnnounceResponse> GetAnnounceResponse(UdpClient udpClient, IPEndPoint endPoint, string announce)
    {
        // Form Announce request to send
        var transactionId = (uint)RandomNumberGenerator.GetInt32(1,int.MaxValue);
        var announceRequest = new UdpTrackerAnnounceRequest(
            transactionId: transactionId, connectionId: _connectionId, infoHash: infoHash,
            peerId: peerId, downloaded: 0, left: left, uploaded: 0, key: key,
            numWant: peersWanted, 
            (ushort)(short) endPoint.Port
        );

        var response = await announceRequest.SendAsync(udpClient, endPoint, announce);
        return response;
    }
    
    
    // merge all the ipv4 and ipv6 peers if no error occurred 
    private static UdpTrackerAnnounceResponse MergeAllIpv4AndIpv6Peers(List<UdpTrackerAnnounceResponse> responses)
    {
        ConcurrentDictionary<IPEndPoint, Peer> superSet = [];
        var lastFailureReason = "";
        
        foreach (var response in responses)
        {
            if (response.FailureReason != null)
            {
                lastFailureReason = response.FailureReason;
                continue;
            }
            superSet = MergePeersDictionary(superSet, response.Peers, int.MaxValue);
        }

        if (superSet.IsEmpty) return new UdpTrackerAnnounceResponse(lastFailureReason);
        return new UdpTrackerAnnounceResponse(responses[0].TransactionId, responses[0].Interval, superSet);
    }
}