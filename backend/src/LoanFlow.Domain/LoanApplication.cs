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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requestedAmount, "requestedAmount");
        if (decimal.Round(requestedAmount, 2) != requestedAmount)
            throw new ArgumentOutOfRangeException("requestedAmount", requestedAmount, "Amount allows at most 2 decimals.");
        if (requestedAmount >= 10_000_000_000_000_000m)
            throw new ArgumentOutOfRangeException("requestedAmount", requestedAmount, "Amount is too large.");
        RequestedAmount = requestedAmount;
    }
}
