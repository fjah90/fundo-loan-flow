namespace LoanFlow.Domain;

public sealed class BlacklistedSsnRule(ISsnBlacklist blacklist) : IDenyRule
{
    public DenialReason? Evaluate(LoanApplicant applicant) =>
        blacklist.Contains(applicant.Ssn) ? DenialReason.NotEligible : null;
}
