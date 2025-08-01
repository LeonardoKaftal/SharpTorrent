using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharpTorrent.P2P;

namespace SharpTorrent.Utils;

public static class Utils
{

    // implement a timer for UDP receive response based on the second passed by parameter
    public static async Task<UdpReceiveResult?> UdpReceiveAsyncWithTimer(UdpClient udpClient, int seconds)
    {
        using var cst = new CancellationTokenSource();
        var timer = Task.Delay(TimeSpan.FromSeconds(seconds), cst.Token);
        var receivedPacket = udpClient.ReceiveAsync();

        var completedTask = await Task.WhenAny(receivedPacket, timer);
        if (completedTask != receivedPacket) return null;
        await cst.CancelAsync();
        return await receivedPacket;
    }

    // utils method used in unit test for quickly concatenating bytes array using concat()
    // Big Endian is needed, but BitConverter in some architecture will convert to little endian,
    // if that's the case the bytes need to be reversed  
    public static byte[] ReverseIfLittleEndian(byte[] bytes)
    {
        if (!BitConverter.IsLittleEndian) return bytes;
        
        var result = new byte[bytes.Length];
        Array.Copy(bytes, result, bytes.Length);
        Array.Reverse(result);
        return result;
    }
    
    public static ConcurrentDictionary<IPEndPoint, Peer> MergePeersDictionary(
        ConcurrentDictionary<IPEndPoint, Peer> first,
        ConcurrentDictionary<IPEndPoint, Peer> second,
        int maxConns)
    {
        foreach (var peer in second)
        {
            if (first.Count >= maxConns) return first;

            var actualKey = peer.Key;

            if (peer.Key is { AddressFamily: AddressFamily.InterNetworkV6, Address.IsIPv4MappedToIPv6: true })
            {
                var ipv4 = peer.Key.Address.MapToIPv4();
                actualKey = new IPEndPoint(ipv4, peer.Key.Port);
            }

            if (!first.ContainsKey(actualKey))
                first[actualKey] = peer.Value;
        }
        return first;
    }
}