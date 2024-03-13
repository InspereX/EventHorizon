using System.Text;

namespace Insperex.EventHorizon.Abstractions.Serialization.Compression
{
    public static class CompressionExtensions
    {
        public static void Compress<T>(this ICompressible<T> compressible, CompressionType? compressionType) where T : class
        {
            if (compressionType == null || compressible.CompressionType != null)
                return;

            // Serialize
            var json = typeof(T) != typeof(string)
                ? SerializationConstants.Serializer.Serialize(compressible.Payload)
                : compressible.Payload as string;

            // Compress
            var compressor = SerializationConstants.CompressionDict[compressionType.Value];
            var bytes = Encoding.UTF8.GetBytes(json);

            // Set Fields
            compressible.CompressionType = compressionType;
            compressible.Data = compressor.Compress(bytes);
            compressible.Payload = null;
        }

        public static void Decompress<T>(this ICompressible<T> compressible) where T : class
        {
            if (compressible.CompressionType == null)
                return;

            // Decompress
            var compressor = SerializationConstants.CompressionDict[compressible.CompressionType.Value];
            var bytes = compressor.Decompress(compressible.Data);
            var json = Encoding.UTF8.GetString(bytes);

            // Deserialize
            compressible.Payload = typeof(T) != typeof(string)
                ? SerializationConstants.Serializer.Deserialize<T>(json)
                : json as T;

            // Set Fields
            compressible.CompressionType = null;
            compressible.Data = null;
        }
    }
}
