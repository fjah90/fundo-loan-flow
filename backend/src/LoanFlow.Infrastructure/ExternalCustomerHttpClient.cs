using System.Net.Http.Json;
using LoanFlow.Application;

namespace LoanFlow.Infrastructure;

/// <summary>HTTP adapter for the external customer service; the mock upserts by customerId.</summary>
public sealed class ExternalCustomerHttpClient(HttpClient http) : IExternalCustomerService
{
    public async Task CreateAsync(CustomerSyncRequested customer, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("customers", customer, OutboxJson.Options, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAsync(CustomerSyncRequested customer, CancellationToken ct)
    {
        using var response = await http.PutAsJsonAsync($"customers/{customer.CustomerId}", customer, OutboxJson.Options, ct);
        response.EnsureSuccessStatusCode();
    }
}
