namespace LoanFlow.Application;

public interface IUnitOfWork
{
    /// <exception cref="DuplicateCustomerException">A unique index rejected the customer.</exception>
    Task CommitAsync(CancellationToken ct = default);
}
