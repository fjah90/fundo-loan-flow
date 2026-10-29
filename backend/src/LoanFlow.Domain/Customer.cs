namespace LoanFlow.Domain;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Ssn Ssn { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public string Company { get; private set; } = null!;
    public LoanApplication Application { get; private set; } = null!;

    private Customer() { }

    public static Customer Create(
        string? firstName, string? lastName, Ssn ssn, Address address, string? company, decimal requestedAmount)
    {
        ArgumentNullException.ThrowIfNull(ssn);
        ArgumentNullException.ThrowIfNull(address);
        var first = Required(firstName, "firstName");
        var last = Required(lastName, "lastName");
        var comp = Required(company, "company");

        var id = Guid.CreateVersion7();
        return new Customer
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Ssn = ssn,
            Address = address,
            Company = comp,
            Application = new LoanApplication(id, requestedAmount),
        };
    }

    public void UpdateFrom(
        string? firstName, string? lastName, Address address, string? company, decimal requestedAmount)
    {
        ArgumentNullException.ThrowIfNull(address);
        var first = Required(firstName, "firstName");
        var last = Required(lastName, "lastName");
        var comp = Required(company, "company");

        // Throws before any mutation if the amount is invalid, so nothing is half-updated.
        Application.ChangeAmount(requestedAmount);
        FirstName = first;
        LastName = last;
        Address = address;
        Company = comp;
    }

    private static string Required(string? value, string paramName)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new DomainValidationException(paramName, $"{paramName} is required.");
        return text;
    }
}
