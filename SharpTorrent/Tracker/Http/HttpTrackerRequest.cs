using System.Text;
using Microsoft.Extensions.Logging;
using SharpTorrent.Utils;

namespace SharpTorrent.Tracker.Http;

public class HttpTrackerRequest(
    byte[] infoHash,
    string peerId,
    ushort port,
    ulong uploaded,
    ulong downloaded,
    ulong left,
    ushort @event)
{
    private readonly string PeerId = peerId;
    private readonly ulong Left = left;
    private readonly uint _event = @event;
    // BEP 23
    private const uint Compact = 1;
    

    private Uri BuildAnnounce(string announceUrl)
    {
        var urlBuilder = new StringBuilder(announceUrl);
        
        var hasQuery = announceUrl.Contains('?');
        urlBuilder.Append(hasQuery ? '&' : '?');
        
        urlBuilder.Append("peer_id=").Append(BytesToPercentageEncoding(Encoding.UTF8.GetBytes(PeerId)));
        urlBuilder.Append("&info_hash=").Append(BytesToPercentageEncoding(infoHash));
        urlBuilder.Append("&port=").Append(port);
        urlBuilder.Append("&left=").Append(Left);
        urlBuilder.Append("&downloaded=").Append(downloaded);
        urlBuilder.Append("&uploaded=").Append(uploaded);
        urlBuilder.Append("&event=started");
        urlBuilder.Append("&compact=").Append(Compact);
        
        return new Uri(urlBuilder.ToString());
    }
    
    // percentage encoding
    private static string BytesToPercentageEncoding(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append('%');
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    public async Task<HttpTrackerResponse> SendRequestAsync(string announce)
    {
        var uri = BuildAnnounce(announce);
        Singleton.Logger.LogInformation("Sending request to tracker: {Uri}", uri);
        HttpResponseMessage response;
        try
        {
            response = await Singleton.HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            return new HttpTrackerResponse($"Tracker {announce} thrown an http exception: " + ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return new HttpTrackerResponse($"Tracker {announce} did not responded in time: " + ex.Message);
        }
        
        var bencodeBytes = await response.Content.ReadAsByteArrayAsync();
        return new HttpTrackerResponse(bencodeBytes, announce);
    }
}
