using System.Text;
using Microsoft.Extensions.Logging;

namespace SharpTorrent.Tracker;

public class TrackerRequest(
    byte[] infoHash,
    string peerId,
    ushort port,
    ulong uploaded,
    ulong downloaded,
    ulong left,
    ushort @event)
{
    public string PeerId = peerId;
    public ulong Left = left;
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
    private string BytesToPercentageEncoding(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append('%');
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
    }

    public async Task<TrackerResponse> SendRequestAsync(string announce)
    {
        var uri = BuildAnnounce(announce);
        Singleton.Logger.LogInformation("Sending request to tracker: {Uri}", uri);

        
        try
        {
            var response = await Singleton.HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            
            var bencodeBytes = await response.Content.ReadAsByteArrayAsync();
            return new TrackerResponse(bencodeBytes);
        }
        catch (Exception ex)
        {
            var error = $"Error while trying to connect to tracker: {announce} because: {ex.Message}";
            Singleton.Logger.LogError("Error while trying to connect to tracker {Announce} because: {Ex}", announce, ex.Message);
            return new TrackerResponse(0, [], error);
        }
    }
}
