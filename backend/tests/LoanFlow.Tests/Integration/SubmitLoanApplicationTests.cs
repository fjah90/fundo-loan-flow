using System.Data.Common;
using System.Text.Json;
using LoanFlow.Application;
using LoanFlow.Domain;
using LoanFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LoanFlow.Tests.Integration;

public sealed class SubmitLoanApplicationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string BlacklistedSsn = "111-11-1111";
    private const string ValidSsn = "555-12-3456";

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task HandleAsync_NewCustomer_ApprovesAndWritesCustomerApplicationAndCreatedOutbox()
    {
        // Arrange
        await using var db = postgres.CreateContext();
        var useCase = CreateUseCase(db);

        // Act
        var result = await useCase.HandleAsync(Command(), TestContext.Current.CancellationToken);

        // Assert
        var approved = Assert.IsType<SubmissionApproved>(result);
        Assert.False(approved.IsReturningCustomer);
        await using var verify = postgres.CreateContext();
        var customer = Assert.Single(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
        var application = Assert.Single(await verify.Set<LoanApplication>().ToListAsync(TestContext.Current.CancellationToken));
        var outbox = Assert.Single(await verify.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(approved.CustomerId, customer.Id);
        Assert.Equal(approved.ApplicationId, application.Id);
        Assert.Equal(customer.Id, application.CustomerId);
        Assert.Equal("CustomerSyncRequested", outbox.Type);
        using var payload = JsonDocument.Parse(outbox.Payload);
        Assert.Equal("Created", payload.RootElement.GetProperty("kind").GetString());
        var root = payload.RootElement;
        Assert.Equal(approved.CustomerId, root.GetProperty("customerId").GetGuid());
        Assert.Equal("Ada", root.GetProperty("firstName").GetString());
        Assert.Equal("Lovelace", root.GetProperty("lastName").GetString());
        Assert.Equal("555123456", root.GetProperty("ssn").GetString());
        Assert.Equal("1 Main St", root.GetProperty("street").GetString());
        Assert.Equal("Beverly Hills", root.GetProperty("city").GetString());
        Assert.Equal("CA", root.GetProperty("state").GetString());
        Assert.Equal("90210", root.GetProperty("zipCode").GetString());
        Assert.Equal("Analytical Engines", root.GetProperty("company").GetString());
        var app = root.GetProperty("application");
        Assert.Equal(approved.ApplicationId, app.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Number, app.GetProperty("requestedAmount").ValueKind);
        Assert.Equal(5000m, app.GetProperty("requestedAmount").GetDecimal());
    }

    [Fact]
    public async Task HandleAsync_SameSsnTwice_UpdatesExistingRowsAndStagesUpdatedOutbox()
    {
        // Arrange
        await using var first = postgres.CreateContext();
        var firstResult = await CreateUseCase(first).HandleAsync(Command(), TestContext.Current.CancellationToken);
        var created = Assert.IsType<SubmissionApproved>(firstResult);
        var returning = Command(
            firstName: "Grace", lastName: "Hopper", ssn: "555123456", street: "9 Oak Ave", city: "Dallas",
            state: "TX", zipCode: "75001", company: "Navy", amount: 9000.50m);
        await using var second = postgres.CreateContext();

        // Act
        var result = await CreateUseCase(second).HandleAsync(returning, TestContext.Current.CancellationToken);

        // Assert
        var updated = Assert.IsType<SubmissionApproved>(result);
        Assert.True(updated.IsReturningCustomer);
        Assert.Equal(created.CustomerId, updated.CustomerId);
        Assert.Equal(created.ApplicationId, updated.ApplicationId);
        await using var verify = postgres.CreateContext();
        var customer = Assert.Single(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
        var application = Assert.Single(await verify.Set<LoanApplication>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Grace", customer.FirstName);
        Assert.Equal("Hopper", customer.LastName);
        Assert.Equal("9 Oak Ave", customer.Address.Street);
        Assert.Equal("Dallas", customer.Address.City);
        Assert.Equal("TX", customer.Address.State.Code);
        Assert.Equal("75001", customer.Address.ZipCode);
        Assert.Equal("Navy", customer.Company);
        Assert.Equal(9000.50m, application.RequestedAmount);
        var outbox = await verify.OutboxMessages.OrderBy(m => m.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, outbox.Count);
        using var payload = JsonDocument.Parse(outbox[^1].Payload);
        Assert.Equal("Updated", payload.RootElement.GetProperty("kind").GetString());
        Assert.Equal(created.CustomerId, payload.RootElement.GetProperty("customerId").GetGuid());
        Assert.Equal(9000.50m, payload.RootElement.GetProperty("application").GetProperty("requestedAmount").GetDecimal());
    }

    [Fact]
    public async Task HandleAsync_DeniedStateWithEmptyFirstName_ThrowsValidationBeforeRules()
    {
        // Arrange
        await using var db = postgres.CreateContext();
        var useCase = CreateUseCase(db);

        // Act
        var act = () => useCase.HandleAsync(Command(state: "NY", firstName: ""), TestContext.Current.CancellationToken);

        // Assert
        var ex = await Assert.ThrowsAsync<DomainValidationException>(act);
        Assert.Equal("firstName", ex.Field);
    }

    [Fact]
    public async Task HandleAsync_BlacklistedSsnWithInvalidAmount_ThrowsValidationBeforeRules()
    {
        // Arrange
        await using var db = postgres.CreateContext();
        var useCase = CreateUseCase(db);

        // Act
        var act = () => useCase.HandleAsync(Command(ssn: BlacklistedSsn, amount: 0.001m), TestContext.Current.CancellationToken);

        // Assert
        var ex = await Assert.ThrowsAsync<DomainValidationException>(act);
        Assert.Equal("requestedAmount", ex.Field);
    }

    [Fact]
    public async Task HandleAsync_ApprovedThenSameSsnInNewYork_DeniesAndKeepsFirstSubmission()
    {
        // Arrange
        await using var first = postgres.CreateContext();
        Assert.IsType<SubmissionApproved>(await CreateUseCase(first).HandleAsync(Command(), TestContext.Current.CancellationToken));
        var second = Command(
            firstName: "Grace", lastName: "Hopper", street: "9 Oak Ave", city: "Albany",
            state: "NY", zipCode: "12207", company: "Navy", amount: 9000m);
        await using var db = postgres.CreateContext();

        // Act
        var result = await CreateUseCase(db).HandleAsync(second, TestContext.Current.CancellationToken);

        // Assert
        var denied = Assert.IsType<SubmissionDenied>(result);
        Assert.Equal("STATE_NOT_SERVED", Assert.Single(denied.Reasons).Code);
        await using var verify = postgres.CreateContext();
        var customer = Assert.Single(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
        var application = Assert.Single(await verify.Set<LoanApplication>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await verify.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal("1 Main St", customer.Address.Street);
        Assert.Equal("Beverly Hills", customer.Address.City);
        Assert.Equal("CA", customer.Address.State.Code);
        Assert.Equal("90210", customer.Address.ZipCode);
        Assert.Equal("Analytical Engines", customer.Company);
        Assert.Equal(5000m, application.RequestedAmount);
    }

    [Theory]
    [InlineData("NY", ValidSsn, "STATE_NOT_SERVED")]
    [InlineData("CA", BlacklistedSsn, "NOT_ELIGIBLE")]
    public async Task HandleAsync_DeniedSubmission_ReturnsDeniedAndWritesNothing(
        string state, string ssn, string expectedCode)
    {
        // Arrange
        await using var db = postgres.CreateContext();
        var useCase = CreateUseCase(db);

        // Act
        var result = await useCase.HandleAsync(Command(state: state, ssn: ssn), TestContext.Current.CancellationToken);

        // Assert
        var denied = Assert.IsType<SubmissionDenied>(result);
        Assert.Equal(expectedCode, Assert.Single(denied.Reasons).Code);
        await AssertTablesEmptyAsync();
    }

    [Fact]
    public async Task HandleAsync_FailureAfterOutboxInsert_RollsBackCustomerApplicationAndOutbox()
    {
        // Arrange
        var interceptor = new FailAfterOutboxInsertInterceptor();
        await using var db = postgres.CreateContext(interceptor);
        var useCase = CreateUseCase(db);

        // Act
        var act = () => useCase.HandleAsync(Command(), TestContext.Current.CancellationToken);

        // Assert
        var ex = await Assert.ThrowsAsync<DbUpdateException>(act);
        Assert.Equal(FailAfterOutboxInsertInterceptor.FailureMessage, ex.InnerException?.Message);
        var executed = string.Join('\n', interceptor.Executed);
        Assert.Contains("\"Customers\"", executed);
        Assert.Contains("\"Applications\"", executed);
        Assert.Contains("\"OutboxMessages\"", executed);
        await AssertTablesEmptyAsync();
    }

    [Fact]
    public async Task CommitAsync_SecondContextInsertsSameSsn_ThrowsCustomerConflictException()
    {
        // Arrange
        var ssn = Ssn.Parse(ValidSsn);
        await using var db1 = postgres.CreateContext();
        await using var db2 = postgres.CreateContext();
        new EfCustomerRepository(db1).Add(NewCustomer(ssn));
        new EfCustomerRepository(db2).Add(NewCustomer(ssn));
        await db1.CommitAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => db2.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<CustomerConflictException>(act);
        await using var verify = postgres.CreateContext();
        Assert.Single(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CommitAsync_SecondContextUpdatesSameCustomer_ThrowsCustomerConflictException()
    {
        // Arrange
        var ssn = Ssn.Parse(ValidSsn);
        await using (var seed = postgres.CreateContext())
        {
            new EfCustomerRepository(seed).Add(NewCustomer(ssn));
            await seed.CommitAsync(TestContext.Current.CancellationToken);
        }
        await using var db1 = postgres.CreateContext();
        await using var db2 = postgres.CreateContext();
        var first = await new EfCustomerRepository(db1).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var second = await new EfCustomerRepository(db2).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var address = Address.Create("1 Main St", "Beverly Hills", "CA", "90210");
        first!.UpdateFrom("Ada", "Lovelace", address, "Analytical Engines", 6000m);
        second!.UpdateFrom("Ada", "Lovelace", address, "Analytical Engines", 7000m);
        await db1.CommitAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => db2.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<CustomerConflictException>(act);
        await using var verify = postgres.CreateContext();
        var saved = await new EfCustomerRepository(verify).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        Assert.Equal(6000m, saved!.Application.RequestedAmount);
    }

    [Fact]
    public async Task CommitAsync_FirstChangesNameSecondChangesAmount_ThrowsCustomerConflictException()
    {
        // Arrange
        var ssn = Ssn.Parse(ValidSsn);
        await using (var seed = postgres.CreateContext())
        {
            new EfCustomerRepository(seed).Add(NewCustomer(ssn));
            await seed.CommitAsync(TestContext.Current.CancellationToken);
        }
        await using var db1 = postgres.CreateContext();
        await using var db2 = postgres.CreateContext();
        var first = await new EfCustomerRepository(db1).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var second = await new EfCustomerRepository(db2).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var address = Address.Create("1 Main St", "Beverly Hills", "CA", "90210");
        first!.UpdateFrom("Grace", "Lovelace", address, "Analytical Engines", 5000m);
        second!.UpdateFrom("Ada", "Lovelace", address, "Analytical Engines", 7000m);
        await db1.CommitAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => db2.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<CustomerConflictException>(act);
        await using var verify = postgres.CreateContext();
        var saved = await new EfCustomerRepository(verify).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        Assert.Equal(5000m, saved!.Application.RequestedAmount);
    }

    [Fact]
    public async Task CommitAsync_FirstChangesAmountSecondResubmitsIdentical_ThrowsCustomerConflictException()
    {
        // Arrange
        var ssn = Ssn.Parse(ValidSsn);
        await using (var seed = postgres.CreateContext())
        {
            new EfCustomerRepository(seed).Add(NewCustomer(ssn));
            await seed.CommitAsync(TestContext.Current.CancellationToken);
        }
        await using var db1 = postgres.CreateContext();
        await using var db2 = postgres.CreateContext();
        var first = await new EfCustomerRepository(db1).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var second = await new EfCustomerRepository(db2).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        var address = Address.Create("1 Main St", "Beverly Hills", "CA", "90210");
        first!.UpdateFrom("Ada", "Lovelace", address, "Analytical Engines", 6000m);
        second!.UpdateFrom("Ada", "Lovelace", address, "Analytical Engines", 5000m);
        await db1.CommitAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => db2.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<CustomerConflictException>(act);
        await using var verify = postgres.CreateContext();
        var saved = await new EfCustomerRepository(verify).GetBySsnAsync(ssn, TestContext.Current.CancellationToken);
        Assert.Equal(6000m, saved!.Application.RequestedAmount);
    }

    private static SubmitLoanApplication CreateUseCase(LoanFlowDbContext db) =>
        new(
            new RuleEngine([new NewYorkStateRule(), new BlacklistedSsnRule(new ConfigurationSsnBlacklist([BlacklistedSsn]))]),
            new EfCustomerRepository(db),
            new OutboxEventPublisher(db),
            db);

    private static SubmitLoanApplicationCommand Command(
        string firstName = "Ada",
        string lastName = "Lovelace",
        string ssn = ValidSsn,
        string street = "1 Main St",
        string city = "Beverly Hills",
        string state = "CA",
        string zipCode = "90210",
        string company = "Analytical Engines",
        decimal amount = 5000m) =>
        new(firstName, lastName, ssn, street, city, state, zipCode, company, amount);

    private static Customer NewCustomer(Ssn ssn) =>
        Customer.Create(
            "Ada", "Lovelace", ssn, Address.Create("1 Main St", "Beverly Hills", "CA", "90210"),
            "Analytical Engines", 5000m);

    private async Task AssertTablesEmptyAsync()
    {
        await using var verify = postgres.CreateContext();
        Assert.Empty(await verify.Customers.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await verify.Set<LoanApplication>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await verify.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
    }

    // "Executed", not "Executing": the insert already ran inside the transaction, so only a real rollback leaves the tables empty.
    private sealed class FailAfterOutboxInsertInterceptor : DbCommandInterceptor
    {
        public const string FailureMessage = "Injected failure after the outbox insert.";

        public List<string> Executed { get; } = [];

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            FailIfOutbox(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            FailIfOutbox(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void FailIfOutbox(DbCommand command)
        {
            Executed.Add(command.CommandText);
            if (command.CommandText.Contains("OutboxMessages", StringComparison.Ordinal))
                throw new InvalidOperationException(FailureMessage);
        }
    }
}
