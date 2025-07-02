namespace SharpTorrent.P2P.Message;

public enum MessageType : int
{
    Unknown = -2,
    KeepAlive = -1,
    Choke = 0,
    Unchoke = 1,
    Interested = 2,
    NotInterested = 3,
    Have = 4,
    Bitfield = 5,
    Request = 6,
    Piece = 7,
    Cancel = 8,
    
    
}
 
