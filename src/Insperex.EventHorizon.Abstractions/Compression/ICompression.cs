namespace Insperex.EventHorizon.Abstractions.Compression
{
    public interface ICompression
    {
        public byte[] Compress(byte[] bytes);
        public byte[] Decompress(byte[] bytes);
    }
}
