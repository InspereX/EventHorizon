﻿using System.Globalization;
using System.Threading.Tasks;
using Destructurama;
using Insperex.EventHorizon.Abstractions.Extensions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.EventStreaming.Extensions;
using Insperex.EventHorizon.EventStreaming.InMemory.Extensions;
using Insperex.EventHorizon.EventStreaming.Pulsar.Extensions;
using Insperex.EventHorizon.EventStreaming.Samples.Handlers;
using Insperex.EventHorizon.EventStreaming.Samples.HostedServices;
using Insperex.EventHorizon.EventStreaming.Samples.Models;
using Insperex.EventHorizon.EventStreaming.Subscriptions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Insperex.EventHorizon.EventStreaming.Samples;

public class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Feeds that Generate Data
                services.AddHostedService<Feed1HostedService>();
                services.AddHostedService<Feed2HostedService>();

                services.AddEventHorizon(x =>
                {
                    // Add Stream
                    // x.AddInMemoryEventStream();
                    x.AddPulsarClient(context.Configuration.GetSection("Pulsar").Bind)
                        .AddEventStreaming(s =>
                            s.WithPulsarStream<Event, Feed1PriceChanged>(p =>
                                    p.WithTenantNamespaceTopic("persistent://example/feed1/event"))
                                .WithPulsarStream<Event, Feed1PriceChanged>(p =>
                                    p.WithTenantNamespaceTopic("persistent://example/feed2/event"))
                        );

                    // Add Hosted Subscription
                    x.AddSubscription<PriceChangeTracker, Event>(h =>
                    {
                        h.AddStream<Feed1PriceChanged>();
                        h.AddStream<Feed2PriceChanged>();
                    });
                });

            })
            .UseSerilog((_, config) => { config
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .Destructure.UsingAttributes();
            })
            .UseEnvironment("local")
            .Build()
            .RunAsync();
    }
}
