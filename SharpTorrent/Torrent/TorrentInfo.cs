using System.Numerics;

namespace SharpTorrent.Torrent;

public class TorrentInfo
{
    private readonly Dictionary<string, object> _infoDict;
    public BigInteger PieceLength;
    public string Pieces = string.Empty;
    public string? Name;
    public BigInteger? Length;
    public List<TorrentFile>? Files;
    
    public TorrentInfo(Dictionary<string, object> infoDict)
    {
        _infoDict = infoDict;
        ComposeTorrentInfo();
    }

    private void ComposeTorrentInfo()
    {
        try
        {
            Pieces = _infoDict["pieces"] as string;
            PieceLength = (BigInteger)(_infoDict["piece length"]);
            if (_infoDict.TryGetValue("name", out var name)) Name = (string)(name);
            if (_infoDict.TryGetValue("length", out var length)) Length = (BigInteger) length;
            else if (_infoDict.TryGetValue("files", out var filesObject))
            {
                Files = [];
                
                foreach (var file in (filesObject as IEnumerable<object>)!)
                {
                    var fileDict = (Dictionary<string, object>)(file);
                    if (fileDict.Count == 0) throw new FormatException("Invalid torrent: file tree can't be empty");
                    var lengthField = (BigInteger)(fileDict["length"]);
                    var pathField = (fileDict["path"] as IEnumerable<object> ?? throw new FormatException("Invalid torrent: path field in the files tree is not present"))
                        .Select(path => path.ToString())
                        .ToString();

                    Files.Add(new TorrentFile(lengthField, pathField!));
                }
            }
            // if there is not a files field nor a length field throw an error as is invalid
            else throw new FormatException("Invalid torrent: torrent does not have files field nor a length field");
        }
        catch (KeyNotFoundException ex)
        {
            throw new FormatException("Invalid torrent: ERROR while dereferencing the files tree: " + ex);
        }
    }
}