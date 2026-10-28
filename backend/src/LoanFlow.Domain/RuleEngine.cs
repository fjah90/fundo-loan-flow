namespace LoanFlow.Domain;

public sealed class RuleEngine
{
    private readonly IDenyRule[] _rules;

    public RuleEngine(IEnumerable<IDenyRule> rules)
    {
        _rules = [.. rules];
        if (_rules.Length == 0)
        {
            throw new ArgumentException("At least one deny rule is required.", nameof(rules));
        }
    }

    public Decision Evaluate(LoanApplicant applicant)
    {
        var reasons = _rules
            .Select(rule => rule.Evaluate(applicant))
            .OfType<DenialReason>()
            .ToList();
        return reasons.Count == 0 ? Decision.Approved : Decision.Denied(reasons);
    }
}
