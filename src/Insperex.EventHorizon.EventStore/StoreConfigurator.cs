using System;
using Insperex.EventHorizon.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Insperex.EventHorizon.EventStore
{
    public class StoreConfigurator<T>
    {
        public IServiceCollection Collection { get; set; }

        public StoreConfigurator(IServiceCollection collection)
        {
            Collection = collection;
        }
    }
}
