using LoanFlow.Domain;

namespace LoanFlow.Application;

public interface ICustomerRepository
{
    Task<Customer?> GetBySsnAsync(Ssn ssn, CancellationToken ct = default);
    void Add(Customer customer);
}
