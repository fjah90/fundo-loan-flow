namespace LoanFlow.Domain;

public sealed class NewYorkStateRule : IDenyRule
{
    public DenialReason? Evaluate(LoanApplicant applicant) =>
        applicant.State.Code == "NY" ? DenialReason.StateNotServed : null;
}
