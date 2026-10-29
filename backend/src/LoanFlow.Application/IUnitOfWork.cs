namespace LoanFlow.Application;

public interface IUnitOfWork
{
    /// <exception cref="CustomerConflictException">A unique index or a concurrent update rejected the customer.</exception>
    Task CommitAsync(CancellationToken ct = default);
}
