namespace SharpTorrent.P2P;

public static class Bitfield
{
    public static bool HasPiece(byte[] bitfield, int bitIndex)
    {
        var byteIndex = bitIndex / 8;
        var offset = bitIndex % 8;
        return (bitfield[byteIndex] >> (7 - offset) & 1) == 1;
    }

    public static void SetPiece(byte[] bitfield, int bitIndex)
    {
        var byteIndex = bitIndex / 8;
        var offset = bitIndex % 8;
        bitfield[byteIndex] |= (byte)(1 << (7 - offset));
    }
}