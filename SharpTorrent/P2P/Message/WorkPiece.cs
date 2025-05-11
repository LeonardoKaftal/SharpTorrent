namespace SharpTorrent.P2P.Message;

public record WorkPiece(int index, byte[] hash, uint length) {}