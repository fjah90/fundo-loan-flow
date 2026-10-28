using LoanFlow.Domain;

namespace LoanFlow.Infrastructure;

public sealed class ConfigurationSsnBlacklist(IEnumerable<string> ssns) : ISsnBlacklist
{
    private readonly HashSet<Ssn> _ssns = [.. ssns.Select(Ssn.Parse)];

    public bool Contains(Ssn ssn) => _ssns.Contains(ssn);
}
