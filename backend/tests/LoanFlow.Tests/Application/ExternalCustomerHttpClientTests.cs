using System.Net;
using System.Text.Json;
using LoanFlow.Application;
using LoanFlow.Infrastructure;
using Xunit;

namespace LoanFlow.Tests.Application;

public sealed class ExternalCustomerHttpClientTests
{
    [Fact]
    public async Task CreateAsync_Created_PostsSnapshotToCustomers()
    {
        // Arrange
        var handler = new CapturingHandler(HttpStatusCode.Created);
        var client = CreateClient(handler);
        var customer = Event(CustomerSyncKind.Created);

        // Act
        await client.CreateAsync(customer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("http://mock/customers", handler.Uri?.ToString());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Created", body.RootElement.GetProperty("kind").GetString());
        Assert.Equal(customer.CustomerId, body.RootElement.GetProperty("customerId").GetGuid());
        Assert.Equal(5000m, body.RootElement.GetProperty("application").GetProperty("requestedAmount").GetDecimal());
    }

    [Fact]
    public async Task UpdateAsync_Updated_PutsSnapshotToCustomerById()
    {
        // Arrange
        var handler = new CapturingHandler(HttpStatusCode.OK);
        var client = CreateClient(handler);
        var customer = Event(CustomerSyncKind.Updated);

        // Act
        await client.UpdateAsync(customer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal($"http://mock/customers/{customer.CustomerId}", handler.Uri?.ToString());
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Updated", body.RootElement.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task CreateAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var client = CreateClient(new CapturingHandler(HttpStatusCode.ServiceUnavailable));

        // Act
        var act = () => client.CreateAsync(Event(CustomerSyncKind.Created), TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(act);
    }

    [Fact]
    public async Task HandleAsync_CreatedKind_CallsOnlyCreate()
    {
        // Arrange
        var external = new RecordingExternal();
        var handler = new SyncCustomerWithExternalService(external);
        var customer = Event(CustomerSyncKind.Created);

        // Act
        await handler.HandleAsync(customer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Create", customer.CustomerId)], external.Calls);
    }

    [Fact]
    public async Task HandleAsync_UpdatedKind_CallsOnlyUpdate()
    {
        // Arrange
        var external = new RecordingExternal();
        var handler = new SyncCustomerWithExternalService(external);
        var customer = Event(CustomerSyncKind.Updated);

        // Act
        await handler.HandleAsync(customer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([("Update", customer.CustomerId)], external.Calls);
    }

    private static ExternalCustomerHttpClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://mock/") });

    private static CustomerSyncRequested Event(CustomerSyncKind kind) =>
        new(kind, Guid.NewGuid(), "Ada", "Lovelace", "555123456", "1 Main St", "Beverly Hills", "CA", "90210",
            "Analytical Engines", new ApplicationSnapshot(Guid.NewGuid(), 5000m));

    private sealed class CapturingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status);
        }
    }

    private sealed class RecordingExternal : IExternalCustomerService
    {
        public List<(string Method, Guid CustomerId)> Calls { get; } = [];

        public Task CreateAsync(CustomerSyncRequested customer, CancellationToken ct)
        {
            Calls.Add(("Create", customer.CustomerId));
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CustomerSyncRequested customer, CancellationToken ct)
        {
            Calls.Add(("Update", customer.CustomerId));
            return Task.CompletedTask;
        }
    }
}
