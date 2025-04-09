using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SharpTorrent.Utils;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker.Udp;

public class UdpTrackerConnectionRequest(byte[] infoHash, string peerId, ulong left, int peersWanted)
{
    private long _connectionId = 0;
    
    public async Task<UdpTrackerAnnounceResponse> SendAsync(string announce)
    {
        var transactionId = new Random().Next();
        
        // Connection request
        var connectionRequest = FormConnectionRequest(transactionId);
        var udpClient = new UdpClient();
        var uri = new Uri(announce);
        IPEndPoint endPoint;
        UdpReceiveResult? receivedResponse;
        
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);
            var receivedIp = addresses.FirstOrDefault(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork);
            
            if (receivedIp == null) return new UdpTrackerAnnounceResponse(0, 0, [], $"Impossible to find IPV4 for tracker {announce}");
            
            endPoint = new IPEndPoint(receivedIp, uri.Port);
            await udpClient.SendAsync(connectionRequest, endPoint);
            
            receivedResponse = await UdpReceiveAsyncWithTimer(udpClient, 5) ?? await UdpReceiveAsyncWithTimer(udpClient, 10);
            
            if (receivedResponse == null) return new UdpTrackerAnnounceResponse(0, 0, [], $"Tracker {announce} did not respond in time to UDP connection request");
        }
        catch (Exception ex)
        {
            return new UdpTrackerAnnounceResponse(0, 0, [], $"Error sending connection request or trying to parse IP trough DNS of {announce}: {ex.Message}");
        }
        
        if (!ConnectionResponseIsValid(receivedResponse?.Buffer!, transactionId)) 
        {
            var error = $"Tracker {announce} have not sent a valid response to UDP connection request";
            return new UdpTrackerAnnounceResponse(0, 0, [], error);
        }
        
        // Form Announce request to send
        transactionId = new Random().Next();
        var announceRequest = new UdpTrackerAnnounceRequest(
            transactionId: transactionId, connectionId: _connectionId, infoHash: infoHash,
            peerId: peerId, downloaded: 0, left: left, uploaded: 0, key: (uint) new Random().Next(),
            numWant: peersWanted, (ushort)(short) uri.Port
        );

        var response = await announceRequest.SendAsync(udpClient, endPoint);
        return response;
    }

    private static byte[] FormConnectionRequest(int transactionId)
    {
        const long protocolId = 0x41727101980L;
        const int action = 0; // connect
        var buffer = ReverseIfLittleEndian(BitConverter.GetBytes(protocolId))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(action)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(transactionId)))
            .ToArray();
        return buffer;
    }

    private bool ConnectionResponseIsValid(byte[] buff, int transactionId)
    {
        if (buff.Length != 16) return false;
        var action = BigEndianToInt32(buff[0..4]);
        if (action != 0) return false;
        var receivedTransactionId = BigEndianToInt32(buff[4..8]);
        if (receivedTransactionId != transactionId) return false;
        _connectionId = BigEndianToInt64(buff[8..16]);
        Singleton.Logger.LogInformation("ATTENTION: SENT A VALID RESPONSE, PROCEDING WITH IT");
        return true;
    }
}