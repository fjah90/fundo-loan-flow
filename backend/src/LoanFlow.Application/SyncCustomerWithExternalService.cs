namespace LoanFlow.Application;

public sealed class SyncCustomerWithExternalService(IExternalCustomerService external)
{
    public Task HandleAsync(CustomerSyncRequested e, CancellationToken ct) => e.Kind switch
    {
        CustomerSyncKind.Created => external.CreateAsync(e, ct),
        CustomerSyncKind.Updated => external.UpdateAsync(e, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.Kind, "Unknown customer sync kind."),
    };
}
