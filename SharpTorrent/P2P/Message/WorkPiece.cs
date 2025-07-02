namespace SharpTorrent.P2P.Message;

public record WorkPiece(int Index, byte[] Hash, uint Length) {}