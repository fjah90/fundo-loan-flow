using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoanFlow.Application;
using LoanFlow.Domain;
using LoanFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LoanFlow.Tests.Integration;

public sealed class LoanApplicationEndpointTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Route = "/api/loan-applications";
    private const string ValidSsn = "555-12-3456";
    private const string BlacklistedSsn = "222-22-2222"; // from appsettings.json (AR-45)

    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:LoanFlow", postgres.ConnectionString);
            b.UseSetting("Outbox:Enabled", "false");
        });

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Post_ValidNewCustomer_Returns200Approved()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var root = body.RootElement;
        Assert.Equal("Approved", root.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, root.GetProperty("applicationId").GetGuid());
        Assert.NotEqual(Guid.Empty, root.GetProperty("customerId").GetGuid());
        Assert.False(root.GetProperty("isReturningCustomer").GetBoolean());
    }

    [Fact]
    public async Task Post_StateNewYork_Returns200DeniedStateNotServed()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(state: "NY"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        Assert.Equal("Denied", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("STATE_NOT_SERVED", body.RootElement.GetProperty("reasons")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_BlacklistedSsnFromAppSettings_Returns200DeniedNotEligibleWithoutLeaks()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(ssn: BlacklistedSsn), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(raw);
        Assert.Equal("Denied", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("NOT_ELIGIBLE", body.RootElement.GetProperty("reasons")[0].GetProperty("code").GetString());
        Assert.DoesNotContain("blacklist", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(BlacklistedSsn, raw, StringComparison.Ordinal);
        Assert.DoesNotContain(BlacklistedSsn.Replace("-", ""), raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_SameSsnTwice_ReturnsReturningCustomerAndKeepsOneRowPerTable()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var first = await client.PostAsJsonAsync(Route, Payload(), TestContext.Current.CancellationToken);
        using var firstBody = await ReadJsonAsync(first);

        // Act
        var second = await client.PostAsJsonAsync(
            Route, Payload(firstName: "Grace", requestedAmount: 9000), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var secondBody = await ReadJsonAsync(second);
        Assert.False(firstBody.RootElement.GetProperty("isReturningCustomer").GetBoolean());
        Assert.True(secondBody.RootElement.GetProperty("isReturningCustomer").GetBoolean());
        Assert.Equal(
            firstBody.RootElement.GetProperty("customerId").GetGuid(),
            secondBody.RootElement.GetProperty("customerId").GetGuid());
        await using var verify = postgres.CreateContext();
        Assert.Single(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await verify.Set<LoanApplication>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await verify.OutboxMessages.CountAsync(TestContext.Current.CancellationToken));
        var customer = await verify.Customers.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Grace", customer.FirstName);
        var application = await verify.Set<LoanApplication>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(9000m, application.RequestedAmount);
    }

    [Fact]
    public async Task Post_MalformedSsn_Returns400ProblemJsonWithSsnError()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(ssn: "12-345"), TestContext.Current.CancellationToken);

        // Assert
        await AssertValidationErrorAsync(response, "ssn");
    }

    [Fact]
    public async Task Post_MissingRequestedAmount_Returns400ProblemJsonWithAmountError()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var payload = Payload();
        payload.Remove("requestedAmount");

        // Act
        var response = await client.PostAsJsonAsync(Route, payload, TestContext.Current.CancellationToken);

        // Assert
        await AssertValidationErrorAsync(response, "requestedAmount");
    }

    [Fact]
    public async Task Post_FirstNameOf101Characters_Returns400ProblemJsonWithFirstNameError()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            Route, Payload(firstName: new string('a', 101)), TestContext.Current.CancellationToken);

        // Assert
        await AssertValidationErrorAsync(response, "firstName");
    }

    [Fact]
    public async Task Post_NulInFirstName_Returns400ProblemJsonWithFirstNameError()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            Route, Payload(firstName: "Ada\u0000"), TestContext.Current.CancellationToken);

        // Assert
        await AssertValidationErrorAsync(response, "firstName");
    }

    [Fact]
    public async Task Post_NonNumericRequestedAmount_Returns400WithCleanKeyAndNoInternals()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var payload = Payload();
        payload["requestedAmount"] = "abc";

        // Act
        var response = await client.PostAsJsonAsync(Route, payload, TestContext.Current.CancellationToken);

        // Assert
        await AssertValidationErrorAsync(response, "requestedAmount");
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(raw);
        Assert.False(body.RootElement.GetProperty("errors").TryGetProperty("$.requestedAmount", out _));
        Assert.DoesNotContain("LoanFlow", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_TruncatedJson_Returns400ProblemJson()
    {
        // Arrange
        using var client = _factory.CreateClient();
        using var content = new StringContent("{\"ssn\":", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(Route, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_MalformedSsn_ErrorMessageHasNoParameterSuffix()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(ssn: "12-345"), TestContext.Current.CancellationToken);

        // Assert
        using var body = await ReadJsonAsync(response);
        var message = body.RootElement.GetProperty("errors").GetProperty("ssn")[0].GetString();
        Assert.DoesNotContain("(Parameter", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_CommitThrowsCustomerConflict_Returns409ProblemJson()
    {
        // Arrange
        using var factory = FactoryWithCommitThrowing(new CustomerConflictException("dup", new Exception()));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_CommitThrowsUnexpected_Returns500ProblemJsonWithoutDetail()
    {
        // Arrange
        using var factory = FactoryWithCommitThrowing(new InvalidOperationException("Host=secret-db"));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("secret-db", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_CommitThrowsArgumentException_Returns500ProblemJsonWithoutDetail()
    {
        // Arrange
        using var factory = FactoryWithCommitThrowing(new ArgumentException("Host=secret-db", "connectionString"));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(Route, Payload(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("secret-db", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("connectionString", raw, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> FactoryWithCommitThrowing(Exception exception) =>
        _factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(services => services.AddScoped<IUnitOfWork>(_ => new ThrowingUnitOfWork(exception))));

    private sealed class ThrowingUnitOfWork(Exception exception) : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken ct = default) => throw exception;
    }

    [Fact]
    public void Host_OutboxDisabled_DoesNotRegisterOutboxProcessor()
    {
        // Arrange
        var hostedServices = _factory.Services.GetServices<IHostedService>();

        // Act
        var processors = hostedServices.OfType<OutboxProcessor>();

        // Assert
        Assert.Empty(processors);
    }

    [Fact]
    public async Task Host_OutboxEnabled_RegistersOneOutboxProcessor()
    {
        // Arrange
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("ConnectionStrings:LoanFlow", postgres.ConnectionString);
                b.UseSetting("Outbox:Enabled", "true");
            });

        // Act
        var processors = factory.Services.GetServices<IHostedService>().OfType<OutboxProcessor>();

        // Assert
        Assert.Single(processors);
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await ReadJsonAsync(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

    private static Dictionary<string, object> Payload(
        string firstName = "Ada",
        string ssn = ValidSsn,
        string state = "CA",
        decimal requestedAmount = 5000m) =>
        new()
        {
            ["firstName"] = firstName,
            ["lastName"] = "Lovelace",
            ["ssn"] = ssn,
            ["street"] = "1 Main St",
            ["city"] = "Beverly Hills",
            ["state"] = state,
            ["zipCode"] = "90210",
            ["company"] = "Analytical Engines",
            ["requestedAmount"] = requestedAmount,
        };
}
