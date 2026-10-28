namespace LoanFlow.Domain;

public sealed record UsState
{
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS",
        "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC",
        "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
    };

    private UsState(string code) => Code = code;

    public string Code { get; }

    public static UsState Parse(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (code is null || !Codes.Contains(code))
            throw new ArgumentException("State must be a US state code.", "state");
        return new UsState(code);
    }

    public override string ToString() => Code;
}
