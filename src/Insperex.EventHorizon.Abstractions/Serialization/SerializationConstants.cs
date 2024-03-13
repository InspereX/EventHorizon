using System.Collections.Generic;
using Insperex.EventHorizon.Abstractions.Serialization.Compression;
using Insperex.EventHorizon.Abstractions.Serialization.Json;

namespace Insperex.EventHorizon.Abstractions.Serialization
{
    public static class SerializationConstants
    {
        public static readonly ISerializer Serializer = new SystemJsonSerializer();

        public static readonly Dictionary<CompressionType, ICompression> CompressionDict = new();

        static SerializationConstants()
        {
            CompressionDict[CompressionType.Gzip] = new GzipCompression();
        }
    }
}
