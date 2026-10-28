namespace LoanFlow.Domain;

public sealed record DenialReason(string Code)
{
    public static readonly DenialReason StateNotServed = new("STATE_NOT_SERVED");
    public static readonly DenialReason NotEligible = new("NOT_ELIGIBLE");
}
