namespace LoanFlow.Domain;

public interface IDenyRule
{
    DenialReason? Evaluate(LoanApplicant applicant);
}
