using System.Collections.Generic;

namespace Insperex.EventHorizon.Abstractions.Compression
{
    public static class CompressionConstants
    {
        public static readonly ISerializer Serializer = new NewtonsoftJsonSerializer();

        public static readonly Dictionary<CompressionType, ICompression> CompressionDict = new();


        static CompressionConstants()
        {
            CompressionDict[CompressionType.Gzip] = new GzipCompression();
        }
    }
}
