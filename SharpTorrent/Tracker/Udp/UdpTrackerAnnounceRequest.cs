using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using SharpTorrent.Utils;
using static SharpTorrent.Utils.Utils;

namespace SharpTorrent.Tracker.Udp;

public class UdpTrackerAnnounceRequest(
    int transactionId,
    long connectionId,
    byte[] infoHash,
    string peerId,
    ulong downloaded,
    ulong left,
    ulong uploaded,
    uint key,
    int numWant,
    ushort port
    )
{
    private const int Action = 1;
    private int _numWant = numWant;
    private const int Event = 0; // started
    private const int IpAdress = 0;

    private byte[] FormAnnounceRequest()
    {
        if (_numWant == int.MaxValue) _numWant = -1;
        var buffer = ReverseIfLittleEndian(BitConverter.GetBytes(connectionId))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(Action)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(transactionId)))
            .Concat(infoHash) // 20 bytes
            .Concat(Encoding.ASCII.GetBytes(peerId)) // 20 bytes
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(downloaded)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(left)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(uploaded)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(Event)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(IpAdress)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(key)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(-1)))
            .Concat(ReverseIfLittleEndian(BitConverter.GetBytes(port)))
            .ToArray();
        
        return buffer;
    }

    public async Task<UdpTrackerAnnounceResponse> SendAsync(UdpClient udpClient, IPEndPoint endPoint)
    {
        var bytes = FormAnnounceRequest();
        await udpClient.SendAsync(bytes, endPoint);
        UdpReceiveResult? response;
        try
        {
            response = await UdpReceiveAsyncWithTimer(udpClient, 5) ?? await UdpReceiveAsyncWithTimer(udpClient, 10);
        }
        catch (Exception ex)
        {
            return new UdpTrackerAnnounceResponse(0, 0, [],
                "There has been an error trying to receive announce request: " + ex.Message);
        }
        
        Singleton.Logger.LogInformation("VALID UDP TRACKER RESPONDED, trying to parse the peers packet"); 
        return response == null
            ? new UdpTrackerAnnounceResponse(0, 0, [], "Tracker did not respond in time to UDP announce request") 
            : new UdpTrackerAnnounceResponse(transactionId, response?.Buffer!);
    }
}