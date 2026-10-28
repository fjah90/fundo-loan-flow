namespace LoanFlow.Domain;

public sealed class Decision
{
    private Decision(IReadOnlyList<DenialReason> reasons) => Reasons = reasons;

    public IReadOnlyList<DenialReason> Reasons { get; }

    public bool IsApproved => Reasons.Count == 0;

    public static Decision Approved { get; } = new([]);

    public static Decision Denied(IReadOnlyList<DenialReason> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        if (reasons.Count == 0)
        {
            throw new ArgumentException("A denial needs at least one reason.", nameof(reasons));
        }

        return new([.. reasons]);
    }
}
