using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.Abstractions
{
    public class EventHorizonConfigurator
    {
        public readonly IServiceCollection Collection;
        public EventHorizonConfigurator(IServiceCollection collection)
        {
            Collection = collection;
        }
    }
}
