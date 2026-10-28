namespace LoanFlow.Domain;

// Port: the implementation lives outside Domain.
public interface ISsnBlacklist
{
    bool Contains(Ssn ssn);
}
