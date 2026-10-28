using LoanFlow.Application;
using LoanFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanFlow.Infrastructure;

public sealed class EfCustomerRepository(LoanFlowDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetBySsnAsync(Ssn ssn, CancellationToken ct = default) =>
        db.Customers.Include(c => c.Application).FirstOrDefaultAsync(c => c.Ssn == ssn, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
}
