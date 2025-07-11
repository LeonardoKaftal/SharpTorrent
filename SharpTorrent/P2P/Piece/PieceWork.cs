namespace SharpTorrent.P2P.Piece;

public record PieceWork(uint Index, byte[] Hash, uint Length) {}