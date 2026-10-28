using System.Text;
using LoanFlow.Domain;

namespace LoanFlow.Application;

public sealed record SubmitLoanApplicationCommand(
    string FirstName, string LastName, string Ssn, string Street, string City,
    string State, string ZipCode, string Company, decimal RequestedAmount)
{
    // Omits Ssn: it may be unparsed here, so it cannot be masked reliably.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"FirstName = {FirstName}, LastName = {LastName}, Street = {Street}, City = {City}, ")
            .Append($"State = {State}, ZipCode = {ZipCode}, Company = {Company}, RequestedAmount = {RequestedAmount}");
        return true;
    }
}

public abstract record SubmissionResult;

public sealed record SubmissionApproved(Guid CustomerId, Guid ApplicationId, bool IsReturningCustomer) : SubmissionResult;

public sealed record SubmissionDenied(IReadOnlyList<DenialReason> Reasons) : SubmissionResult;

public sealed class SubmitLoanApplication(
    RuleEngine rules, ICustomerRepository customers, IIntegrationEventPublisher events, IUnitOfWork unitOfWork)
{
    public async Task<SubmissionResult> HandleAsync(SubmitLoanApplicationCommand command, CancellationToken ct = default)
    {
        var ssn = Ssn.Parse(command.Ssn);
        var address = Address.Create(command.Street, command.City, command.State, command.ZipCode);
        // Builds the candidate first so every field is validated before the rules run (ARCHITECTURE §3).
        var candidate = Customer.Create(
            command.FirstName, command.LastName, ssn, address, command.Company, command.RequestedAmount);

        var decision = rules.Evaluate(new LoanApplicant(address.State, ssn));
        if (!decision.IsApproved)
            return new SubmissionDenied(decision.Reasons);

        var customer = await customers.GetBySsnAsync(ssn, ct);
        var isReturning = customer is not null;
        if (customer is null)
        {
            customer = candidate;
            customers.Add(customer);
        }
        else
        {
            customer.UpdateFrom(command.FirstName, command.LastName, address, command.Company, command.RequestedAmount);
        }

        events.Publish(CustomerSyncRequested.From(isReturning ? CustomerSyncKind.Updated : CustomerSyncKind.Created, customer));
        await unitOfWork.CommitAsync(ct);
        return new SubmissionApproved(customer.Id, customer.Application.Id, isReturning);
    }
}
