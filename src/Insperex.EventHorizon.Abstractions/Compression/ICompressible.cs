namespace Insperex.EventHorizon.Abstractions.Compression
{
    public interface ICompressible<T> where T : class
    {
        public T Payload { get; set; }
        public CompressionType? CompressionType { get; set; }
        public byte[] Data { get; set; }
    }
}
