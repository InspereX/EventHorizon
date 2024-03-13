using System.Text;

namespace Insperex.EventHorizon.Abstractions.Compression
{
    public static class CompressionExtensions
    {
        public static void Compress<T>(this ICompressible<T> compressible, CompressionType compressionType) where T : class
        {
            if (compressible.CompressionType == CompressionType.None)
                return;

            // Serialize
            var json = CompressionConstants.Serializer.Serialize(compressible.Payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            // Compress
            var compressor = CompressionConstants.CompressionDict[compressionType];
            compressible.CompressionType = compressionType;
            compressible.Data = compressor.Compress(bytes);
            compressible.Payload = null;
        }

        public static void Decompress<T>(this ICompressible<T> compressible) where T : class
        {
            if (compressible.CompressionType == CompressionType.None)
                return;

            // Decompress
            var compressor = CompressionConstants.CompressionDict[compressible.CompressionType];
            var bytes = compressor.Decompress(compressible.Data);
            var json = Encoding.UTF8.GetString(bytes);

            // Deserialize
            compressible.Payload = CompressionConstants.Serializer.Deserialize<T>(json);
            compressible.CompressionType = CompressionType.None;
            compressible.Data = null;
        }
    }
}
