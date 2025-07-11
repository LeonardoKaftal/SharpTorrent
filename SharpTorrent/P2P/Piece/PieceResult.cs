using Microsoft.VisualBasic.CompilerServices;

namespace SharpTorrent.P2P.Piece;

public record PieceResult(uint Index, byte[] Buf){}