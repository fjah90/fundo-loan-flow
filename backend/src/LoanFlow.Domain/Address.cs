using System.Text.RegularExpressions;

namespace LoanFlow.Domain;

public sealed partial record Address
{
    private Address(string street, string city, UsState state, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string Street { get; }
    public string City { get; }
    public UsState State { get; }
    public string ZipCode { get; }

    public static Address Create(string? street, string? city, string? state, string? zipCode)
    {
        var s = street?.Trim();
        if (string.IsNullOrEmpty(s))
            throw new DomainValidationException("street", "Street is required.");
        var c = city?.Trim();
        if (string.IsNullOrEmpty(c))
            throw new DomainValidationException("city", "City is required.");
        var st = UsState.Parse(state);
        var zip = zipCode?.Trim();
        if (zip is null || !Zip().IsMatch(zip))
            throw new DomainValidationException("zipCode", "ZIP must be 12345 or 12345-6789.");
        return new Address(s, c, st, zip);
    }

    [GeneratedRegex(@"^[0-9]{5}(?:-[0-9]{4})?$")]
    private static partial Regex Zip();
}
