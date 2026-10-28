using System.Text.Json;
using System.Text.Json.Serialization;
using LoanFlow.Application;
using LoanFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LoanFlow.Tests.Integration;

public sealed class OutboxProcessorTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ProcessPendingAsync_Success_MarksProcessedAt()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        await SeedAsync(Event(CustomerSyncKind.Created, customerId));
        var external = new RecordingExternal();
        await using var provider = BuildProvider(external);
        var processor = CreateProcessor(provider);

        // Act
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        var call = Assert.Single(external.Calls);
        Assert.Equal(("Create", customerId), call);
        var row = Assert.Single(await ReadRowsAsync());
        Assert.NotNull(row.ProcessedAt);
        Assert.Equal(0, row.Attempts);
    }

    [Fact]
    public async Task ProcessPendingAsync_Failure_KeepsRowPendingAndStopsBatch()
    {
        // Arrange
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await SeedAsync(Event(CustomerSyncKind.Created, firstId), Event(CustomerSyncKind.Created, secondId));
        await MoveLowestIdToHeapEndAsync();
        var external = new RecordingExternal(failOnCall: 1);
        await using var provider = BuildProvider(external);
        var processor = CreateProcessor(provider);

        // Act
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Create", firstId)], external.Calls);
        var rows = await ReadRowsAsync();
        Assert.Equal(2, rows.Count);
        Assert.Null(rows[0].ProcessedAt);
        Assert.Equal(1, rows[0].Attempts);
        Assert.False(string.IsNullOrWhiteSpace(rows[0].LastError));
        Assert.Null(rows[1].ProcessedAt);
        Assert.Equal(0, rows[1].Attempts);
    }

    [Fact]
    public async Task ProcessPendingAsync_FailureThenRetry_DeliversFailedRowThenNextInIdOrder()
    {
        // Arrange
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await SeedAsync(Event(CustomerSyncKind.Created, firstId), Event(CustomerSyncKind.Created, secondId));
        await MoveLowestIdToHeapEndAsync();
        var external = new RecordingExternal(failOnCall: 1);
        await using var provider = BuildProvider(external);
        var processor = CreateProcessor(provider);

        // Act
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);
        var afterFirstPass = await ReadRowsAsync();
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Create", firstId)], external.Calls.Take(1));
        Assert.Null(afterFirstPass[0].ProcessedAt);
        Assert.Equal(1, afterFirstPass[0].Attempts);
        Assert.Null(afterFirstPass[1].ProcessedAt);
        Assert.Equal([("Create", firstId), ("Create", firstId), ("Create", secondId)], external.Calls);
        var rows = await ReadRowsAsync();
        Assert.All(rows, row => Assert.NotNull(row.ProcessedAt));
    }

    [Fact]
    public async Task ProcessPendingAsync_DeliversInIdOrder_CreateBeforeUpdate()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        await SeedAsync(Event(CustomerSyncKind.Created, customerId), Event(CustomerSyncKind.Updated, customerId));
        await MoveLowestIdToHeapEndAsync();
        var external = new RecordingExternal();
        await using var provider = BuildProvider(external);
        var processor = CreateProcessor(provider);

        // Act
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Create", customerId), ("Update", customerId)], external.Calls);
        var rows = await ReadRowsAsync();
        Assert.All(rows, row => Assert.NotNull(row.ProcessedAt));
    }

    [Fact]
    public async Task ProcessPendingAsync_SkipsAlreadyProcessedRows()
    {
        // Arrange
        await SeedAsync(processedAt: DateTimeOffset.UtcNow, Event(CustomerSyncKind.Created, Guid.NewGuid()));
        var external = new RecordingExternal();
        await using var provider = BuildProvider(external);
        var processor = CreateProcessor(provider);

        // Act
        await processor.ProcessPendingAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(external.Calls);
        var row = Assert.Single(await ReadRowsAsync());
        Assert.NotNull(row.ProcessedAt);
        Assert.Equal(0, row.Attempts);
    }

    private static CustomerSyncRequested Event(CustomerSyncKind kind, Guid customerId) =>
        new(kind, customerId, "Ada", "Lovelace", "555123456", "1 Main St", "Beverly Hills", "CA", "90210",
            "Analytical Engines", new ApplicationSnapshot(Guid.NewGuid(), 5000m));

    private Task SeedAsync(params CustomerSyncRequested[] events) => SeedAsync(null, events);

    private async Task SeedAsync(DateTimeOffset? processedAt, params CustomerSyncRequested[] events)
    {
        await using var db = postgres.CreateContext();
        foreach (var e in events)
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = nameof(CustomerSyncRequested),
                Payload = JsonSerializer.Serialize(e, PayloadOptions),
                OccurredAt = DateTimeOffset.UtcNow,
                ProcessedAt = processedAt,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken); // one save per row keeps Id order
        }
    }

    // An UPDATE writes a new tuple version, so the lowest-Id row lands after the others in the heap.
    // Without ORDER BY Id a sequential scan then returns rows out of Id order; the guard proves the premise.
    private async Task MoveLowestIdToHeapEndAsync()
    {
        await using var db = postgres.CreateContext();
        var ct = TestContext.Current.CancellationToken;
        var lowestId = await db.OutboxMessages.MinAsync(m => m.Id, ct);
        await db.OutboxMessages.Where(m => m.Id == lowestId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.OccurredAt, DateTimeOffset.UtcNow), ct);

        var physical = await db.OutboxMessages.AsNoTracking().Select(m => m.Id).ToListAsync(ct);
        Assert.NotEqual(physical.Order().ToList(), physical);
    }

    private async Task<List<OutboxMessage>> ReadRowsAsync()
    {
        await using var db = postgres.CreateContext();
        return await db.OutboxMessages.AsNoTracking().OrderBy(m => m.Id).ToListAsync(TestContext.Current.CancellationToken);
    }

    private ServiceProvider BuildProvider(RecordingExternal external)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<LoanFlowDbContext>(o => o.UseNpgsql(postgres.ConnectionString));
        services.AddScoped<SyncCustomerWithExternalService>();
        services.AddSingleton<IExternalCustomerService>(external);
        return services.BuildServiceProvider();
    }

    private static OutboxProcessor CreateProcessor(ServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxProcessor>.Instance,
            TimeSpan.FromSeconds(2));

    private sealed class RecordingExternal(int failOnCall = 0) : IExternalCustomerService
    {
        public List<(string Method, Guid CustomerId)> Calls { get; } = [];

        public Task CreateAsync(CustomerSyncRequested customer, CancellationToken ct) => Record("Create", customer);

        public Task UpdateAsync(CustomerSyncRequested customer, CancellationToken ct) => Record("Update", customer);

        private Task Record(string method, CustomerSyncRequested customer)
        {
            Calls.Add((method, customer.CustomerId));
            return Calls.Count == failOnCall
                ? throw new HttpRequestException("external service unavailable")
                : Task.CompletedTask;
        }
    }
}
