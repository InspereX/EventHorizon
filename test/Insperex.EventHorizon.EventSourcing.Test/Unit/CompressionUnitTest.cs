using System.Linq;
using Insperex.EventHorizon.Abstractions.Extensions;
using Insperex.EventHorizon.Abstractions.Interfaces.Actions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Serialization;
using Insperex.EventHorizon.Abstractions.Serialization.Compression;
using Xunit;

namespace Insperex.EventHorizon.EventSourcing.Test.Unit
{
    public class CompressionUnitTest
    {
        [Fact]
        public void TestCompression()
        {
            // Act
            var @event = new Event("123", new ExampleEvent { Name = "Name" });
            @event.Compress(CompressionType.Gzip);

            // Assert
            Assert.Null(@event.Payload);
            Assert.NotNull(@event.CompressionType);
            Assert.NotNull(@event.Data);
        }


        [Fact]
        public void TestDecompress()
        {
            // Act
            var @event = new Event("123", new ExampleEvent { Name = "Name" });
            @event.Compress(CompressionType.Gzip);
            @event.Decompress();

            // Assert
            var types = new [] { typeof(ExampleEvent) };
            var entity = @event.GetPayload(types.ToDictionary(x => x.Name)) as ExampleEvent;
            Assert.NotNull(@event.Payload);
            Assert.Null(@event.CompressionType);
            Assert.Null(@event.Data);
            Assert.Equal("Name", entity.Name);
        }

        public class ExampleEvent : IEvent
        {
            public string Name { get; set; }
        }
    }
}
