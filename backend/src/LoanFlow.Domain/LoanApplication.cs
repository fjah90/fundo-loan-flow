namespace LoanFlow.Domain;

public sealed class LoanApplication
{
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
        RequestedAmount = requestedAmount;
    }
}
