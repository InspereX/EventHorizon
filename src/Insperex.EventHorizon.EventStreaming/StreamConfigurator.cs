using Insperex.EventHorizon.EventStreaming.Admins;
using Insperex.EventHorizon.EventStreaming.Publishers;
using Insperex.EventHorizon.EventStreaming.Readers;
using Insperex.EventHorizon.EventStreaming.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStreaming
{
    public class StreamConfigurator
    {
        public IServiceCollection Collection { get; set; }

        public StreamConfigurator(IServiceCollection collection)
        {
            Collection = collection;

            // Common
            Collection.AddSingleton(typeof(StreamingClient<>));
            Collection.AddSingleton(typeof(PublisherBuilder<>));
            Collection.AddSingleton(typeof(ReaderBuilder<>));
            Collection.AddSingleton(typeof(SubscriptionBuilder<>));
            Collection.AddSingleton(typeof(Admin<>));
        }
    }
}
