using Microsoft.Extensions.Logging;

namespace SharpTorrent.Torrent;

public class TorrentInfo
{
    public readonly Dictionary<string, object> InfoDict;
    public uint PieceLength { get; private set; }
    public byte[] Pieces { get; private set; }
    public string? Name { get; private set; }
    public ulong? Length { get; private set; }
    public ushort? Private { get; private set; }
    public List<TorrentFile>? Files { get; private set; }
    
    public TorrentInfo(Dictionary<string, object> infoDict)
    {
        InfoDict = infoDict;
        ComposeTorrentInfo();
    }

    private void ComposeTorrentInfo()
    {
        try
        {
            Pieces = (byte[])InfoDict["pieces"];
            PieceLength = (uint)(int)(long)InfoDict["piece length"];
            if (InfoDict.TryGetValue("name", out var name)) Name = (string)(name);
            if (InfoDict.TryGetValue("private", out var priv))
            {
                Private = (ushort)(short)(int)(long)priv;
                // CRITICAL ERROR, CLIENT DOES NOT SUPPORT PRIVATE TORRENT 
                if (Private == 1) throw new NotSupportedException("UNSUPPORTED TORRENT: CLIENT DOES NOT SUPPORT PRIVATE TORRENT");
            }
            if (InfoDict.TryGetValue("length", out var length)) Length = (ulong)(long)length;
            else if (InfoDict.TryGetValue("files", out var filesObject))
            {
                Files = [];

                foreach (var file in (filesObject as IEnumerable<object>)!)
                {
                    var fileDict = (Dictionary<string, object>)(file);
                    if (fileDict.Count == 0) throw new FormatException("Invalid torrent: file tree can't be empty");
                    var lengthField = (ulong)(long)fileDict["length"];
                    var pathField = string.Join("/", (fileDict["path"] as IEnumerable<object> ?? 
                                                      throw new FormatException("Invalid torrent: path field in the files tree is not present"))
                        .Select(path => path.ToString()));
                    Files.Add(new TorrentFile(lengthField, pathField!));
                }
            }
            // if there is not a files field nor a length field throw an error as is invalid
            else throw new FormatException("Invalid torrent: torrent does not have files field nor a length field");
        }
        catch (KeyNotFoundException ex)
        {
            throw new FormatException("INVALID TORRENT: ERROR while dereferencing Info dictionary, the torrent was malformed because a mandatory field has not been found: " + ex);
        }
        catch (InvalidCastException ex)
        {
            throw new FormatException("INVALID TORRENT: ERROR while dereferencing Info dictionary, the torrent was malformed because a found value was of the wrong type: " + ex);
        }
    }
}