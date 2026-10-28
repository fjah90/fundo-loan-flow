namespace LoanFlow.Application;

/// <summary>Port to the external customer service (HTTP boundary); implemented in Infrastructure.</summary>
public interface IExternalCustomerService
{
    Task CreateAsync(CustomerSyncRequested customer, CancellationToken ct);

    Task UpdateAsync(CustomerSyncRequested customer, CancellationToken ct);
}
