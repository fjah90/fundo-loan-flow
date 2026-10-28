using System.Text;
using LoanFlow.Domain;

namespace LoanFlow.Application;

public enum CustomerSyncKind { Created, Updated }

public sealed record ApplicationSnapshot(Guid Id, decimal RequestedAmount);

public sealed record CustomerSyncRequested(
    CustomerSyncKind Kind, Guid CustomerId, string FirstName, string LastName, string Ssn,
    string Street, string City, string State, string ZipCode, string Company, ApplicationSnapshot Application)
{
    // Ssn here is always valid (built from a Customer); print it masked.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"Kind = {Kind}, CustomerId = {CustomerId}, FirstName = {FirstName}, LastName = {LastName}, ")
            .Append($"Ssn = {Domain.Ssn.Parse(Ssn)}, Street = {Street}, City = {City}, State = {State}, ")
            .Append($"ZipCode = {ZipCode}, Company = {Company}, Application = {Application}");
        return true;
    }

    public static CustomerSyncRequested From(CustomerSyncKind kind, Customer customer) =>
        new(kind, customer.Id, customer.FirstName, customer.LastName, customer.Ssn.Value,
            customer.Address.Street, customer.Address.City, customer.Address.State.Code, customer.Address.ZipCode,
            customer.Company, new ApplicationSnapshot(customer.Application.Id, customer.Application.RequestedAmount));
}
