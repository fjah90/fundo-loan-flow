namespace LoanFlow.Domain;

public sealed class LoanApplication
{
    private LoanApplication() { }

    internal LoanApplication(Guid customerId, decimal requestedAmount)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
        ChangeAmount(requestedAmount);
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal RequestedAmount { get; private set; }

    internal void ChangeAmount(decimal requestedAmount)
    {
        if (requestedAmount <= 0)
            throw new DomainValidationException("requestedAmount", "Amount must be greater than 0.");
        if (decimal.Round(requestedAmount, 2) != requestedAmount)
            throw new DomainValidationException("requestedAmount", "Amount allows at most 2 decimals.");
        if (requestedAmount >= 10_000_000_000_000_000m)
            throw new DomainValidationException("requestedAmount", "Amount is too large.");
        RequestedAmount = requestedAmount;
    }
}
