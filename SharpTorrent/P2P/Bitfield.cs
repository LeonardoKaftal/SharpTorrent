namespace SharpTorrent.P2P;

public static class Bitfield
{
    public static bool HasPiece(byte[] bitfield, uint bitIndex)
    {
        var byteIndex = bitIndex / 8;
        var offset = bitIndex % 8;
        return (bitfield[byteIndex] >> (int)(7 - offset) & 1) == 1;
    }

    public static void SetPiece(byte[] bitfield, uint bitIndex)
    {
        var byteIndex = bitIndex / 8;
        var offset = bitIndex % 8;
        bitfield[byteIndex] |= (byte)(1 << (int)(7 - offset));
    }
}