using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Destructurama;
using Insperex.EventHorizon.Abstractions.Extensions;
using Insperex.EventHorizon.Abstractions.Models.TopicMessages;
using Insperex.EventHorizon.Abstractions.Testing;
using Insperex.EventHorizon.EventSourcing.Extensions;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Actions;
using Insperex.EventHorizon.EventSourcing.Samples.Models.Snapshots;
using Insperex.EventHorizon.EventSourcing.Senders;
using Insperex.EventHorizon.EventSourcing.Test.Fakers;
using Insperex.EventHorizon.EventStore.Extensions;
using Insperex.EventHorizon.EventStore.InMemory.Extensions;
using Insperex.EventHorizon.EventStreaming;
using Insperex.EventHorizon.EventStreaming.Pulsar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace Insperex.EventHorizon.EventSourcing.Test.Integration;

[Trait("Category", "Integration")]
public class SenderIntegrationTest : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly IHost _host;
    private readonly Sender _sender;
    private Stopwatch _stopwatch;
    private readonly Sender _sender2;
    private readonly EventSourcingClient<Account> _eventSourcingClient;
    private readonly StreamingClient _streamingClient;

    public SenderIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
        _host = Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddEventHorizon(x =>
                {
                    x.AddEventSourcing()

                        // Hosts
                        .ApplyRequestsToSnapshot<Account>(a => a.BatchSize(10000))

                        // Stores
                        .AddInMemorySnapshotStore()
                        .AddInMemoryViewStore()
                        // .AddInMemoryEventStream();
                        .AddPulsarEventStream(hostContext.Configuration.GetSection("Pulsar").Bind);
                });
            })
            .UseSerilog((_, config) =>
            {
                config.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                    .WriteTo.TestOutput(output, LogEventLevel.Information, formatProvider: CultureInfo.InvariantCulture)
                    .Destructure.UsingAttributes();
            })
            .UseEnvironment("test")
            .Build()
            .AddTestBucketIds();

        _sender = _host.Services.GetRequiredService<SenderBuilder>()
            .Timeout(TimeSpan.FromSeconds(30))
            .GetErrorResult((req, status, error) => new AccountResponse(status, error))
            .Build();

        _sender2 = _host.Services.GetRequiredService<SenderBuilder>()
            .Timeout(TimeSpan.FromMinutes(1))
            .GetErrorResult((req, status, error) => new AccountResponse(status, error))
            .Build();

        _eventSourcingClient = _host.Services.GetRequiredService<EventSourcingClient<Account>>();
        _streamingClient = _host.Services.GetRequiredService<StreamingClient>();
    }

    public async Task InitializeAsync()
    {
        await _host.StartAsync();

        // // Warmup
        // var streamId = EventSourcingFakers.Faker.Random.AlphaNumeric(10);
        // var events = Enumerable.Range(0, 10).Select(x => new OpenAccount(100)).ToArray();
        // var result1 = await _sender2.SendAndReceiveAsync(streamId, events);


        _stopwatch = Stopwatch.StartNew();
    }

    public async Task DisposeAsync()
    {
        _stopwatch.Stop();
        _output.WriteLine($"Test Ran in {_stopwatch.ElapsedMilliseconds}ms");
        await _eventSourcingClient.GetSnapshotStore().DropDatabaseAsync(CancellationToken.None);
        await _streamingClient.GetAdmin<BatchEvent>().DeleteTopicAsync(typeof(Account));
        await _streamingClient.GetAdmin<BatchRequest>().DeleteTopicAsync(typeof(Account));
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact]
    public async Task TestSendAndReceiveAsync()
    {
        // Send Command
        var streamId = EventSourcingFakers.Faker.Random.AlphaNumeric(10);
        var result1 = await _sender2.SendAndReceiveAsync(streamId, new OpenAccount(1000));
        var result2 = _sender2.SendAndReceiveAsync(streamId, new Withdrawal(100));
        var result3 = _sender2.SendAndReceiveAsync(streamId, new Deposit(100));
        var result4 = _sender.SendAndReceiveAsync("ABC", new OpenAccount(100));
        var result5 = _sender.SendAndReceiveAsync("DFG", new OpenAccount(100));
        await Task.WhenAll(result2, result3, result4, result5);

        // Assert Status
        Assert.True(HttpStatusCode.OK == result1.StatusCode, result1.Error);
        Assert.True(HttpStatusCode.OK == result2.Result.StatusCode, result2.Result.Error);
        Assert.True(HttpStatusCode.OK == result3.Result.StatusCode, result3.Result.Error);
        Assert.True(HttpStatusCode.OK == result4.Result.StatusCode, result4.Result.Error);
        Assert.True(HttpStatusCode.OK == result5.Result.StatusCode, result5.Result.Error);
    }

    [Theory]
    [InlineData(100,10000)]
    [InlineData(10000,100)]
    public async Task TestLargeSendAndReceiveAsync(int batch, int req)
    {
        // Batches
        var batchRequests = new List<BatchRequest>();
        for (var i = 0; i < batch; i++)
        {
            // Requests
            var request = new List<Request>();
            for (var j = 0; j < req; j++)
                request.Add(new Request($"{j}", new Deposit(100)));
            batchRequests.Add(new BatchRequest($"{i}", request.ToArray()));
        }

        var result = await _sender2.SendAndReceiveAsync<Account>(batchRequests.ToArray());

        // Assert Status
        foreach (var response in result)
            Assert.True(response.StatusCode < 300, response.Error);
    }

    [Fact]
    public async Task TestSendAndReceiveFailedAsync()
    {
        // Send Command
        var streamId = EventSourcingFakers.Faker.Random.AlphaNumeric(10);
        var command = new Withdrawal(100);
        var result = await _sender.SendAndReceiveAsync(streamId, command);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal(AccountConstants.WithdrawalDenied, result.Error);
    }

    [Fact]
    public async Task TestSenderTimedOut()
    {
        var sender = _host.Services.GetRequiredService<SenderBuilder>()
            .Timeout(TimeSpan.Zero)
            .GetErrorResult((req, status, error) => new AccountResponse(status, error))
            .Build();

        // Send Command
        var streamId = EventSourcingFakers.Faker.Random.AlphaNumeric(10);
        var command = new OpenAccount(100);
        var result = await sender.SendAndReceiveAsync(streamId, command);
        Assert.Equal(HttpStatusCode.RequestTimeout, result.StatusCode);
    }
}
