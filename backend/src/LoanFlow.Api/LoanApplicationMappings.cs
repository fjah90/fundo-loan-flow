using LoanFlow.Application;

namespace LoanFlow.Api;

public sealed record ApprovedResponse(string Status, Guid ApplicationId, Guid CustomerId, bool IsReturningCustomer);

public sealed record DeniedResponse(string Status, IReadOnlyList<DeniedReason> Reasons);

public sealed record DeniedReason(string Code);

public static class LoanApplicationMappings
{
    public static SubmitLoanApplicationCommand ToCommand(this LoanApplicationRequest r) => new(
        r.FirstName, r.LastName, r.Ssn, r.Street, r.City, r.State, r.ZipCode, r.Company, r.RequestedAmount!.Value);

    public static object ToResponse(this SubmissionResult result) => result switch
    {
        SubmissionApproved a => new ApprovedResponse("Approved", a.ApplicationId, a.CustomerId, a.IsReturningCustomer),
        SubmissionDenied d => new DeniedResponse("Denied", d.Reasons.Select(r => new DeniedReason(r.Code)).ToArray()),
        _ => throw new InvalidOperationException("Unknown submission result."),
    };
}
