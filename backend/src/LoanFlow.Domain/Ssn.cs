using System.Text.RegularExpressions;

namespace LoanFlow.Domain;

public sealed partial record Ssn
{
    private Ssn(string value) => Value = value;

    /// <summary>Nine digits, no dashes.</summary>
    public string Value { get; }

    public static Ssn Parse(string? value)
    {
        var text = value?.Trim();
        if (text is null || !Format().IsMatch(text))
            throw new DomainValidationException("ssn", "SSN must be 123-45-6789 or 9 digits.");
        return new Ssn(text.Replace("-", ""));
    }

    public override string ToString() => "***-**-" + Value[5..];

    [GeneratedRegex(@"^(?:[0-9]{3}-[0-9]{2}-[0-9]{4}|[0-9]{9})$")]
    private static partial Regex Format();
}
