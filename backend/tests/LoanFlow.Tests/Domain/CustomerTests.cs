using System.Globalization;
using LoanFlow.Domain;
using Xunit;

namespace LoanFlow.Tests.Domain;

public class CustomerTests
{
    private static readonly Ssn ValidSsn = Ssn.Parse("123-45-6789");

    private static Address ValidAddress() => Address.Create("1 Main St", "Austin", "TX", "73301");

    private static Customer NewCustomer() =>
        Customer.Create("Ada", "Lovelace", ValidSsn, ValidAddress(), "Analytical Engines", 5000m);

    [Fact]
    public void Create_WithValidData_AssignsVersion7Id()
    {
        // Act
        var customer = NewCustomer();

        // Assert
        Assert.Equal(7, customer.Id.Version);
    }

    [Fact]
    public void Create_WithValidData_SetsCustomerFields()
    {
        // Act
        var customer = NewCustomer();

        // Assert
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal(ValidSsn, customer.Ssn);
        Assert.Equal(ValidAddress(), customer.Address);
        Assert.Equal("Analytical Engines", customer.Company);
    }

    [Fact]
    public void Create_WithValidData_CreatesApplicationLinkedToCustomer()
    {
        // Act
        var customer = NewCustomer();

        // Assert
        Assert.NotNull(customer.Application);
        Assert.Equal(7, customer.Application.Id.Version);
        Assert.Equal(customer.Id, customer.Application.CustomerId);
        Assert.Equal(5000m, customer.Application.RequestedAmount);
    }

    [Fact]
    public void Create_WithPaddedText_TrimsNamesAndCompany()
    {
        // Act
        var customer = Customer.Create("  Ada ", " Lovelace  ", ValidSsn, ValidAddress(), "  Analytical Engines ", 1m);

        // Assert
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal("Analytical Engines", customer.Company);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFirstName_ThrowsValidationErrorForFirstName(string? value)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            Customer.Create(value, "Lovelace", ValidSsn, ValidAddress(), "Acme", 100m));

        // Assert
        Assert.Equal("firstName", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLastName_ThrowsValidationErrorForLastName(string? value)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            Customer.Create("Ada", value, ValidSsn, ValidAddress(), "Acme", 100m));

        // Assert
        Assert.Equal("lastName", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCompany_ThrowsValidationErrorForCompany(string? value)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            Customer.Create("Ada", "Lovelace", ValidSsn, ValidAddress(), value, 100m));

        // Assert
        Assert.Equal("company", ex.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithNonPositiveAmount_ThrowsValidationErrorForRequestedAmount(double amount)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            Customer.Create("Ada", "Lovelace", ValidSsn, ValidAddress(), "Acme", (decimal)amount));

        // Assert
        Assert.Equal("requestedAmount", ex.Field);
        Assert.StartsWith("Amount must be greater than 0.", ex.Message);
    }

    [Fact]
    public void UpdateFrom_WithValidData_ReplacesCustomerFields()
    {
        // Arrange
        var customer = NewCustomer();
        var newAddress = Address.Create("9 Oak Ave", "Dallas", "TX", "75001");

        // Act
        customer.UpdateFrom("Grace", "Hopper", newAddress, "Navy", 9000m);

        // Assert
        Assert.Equal("Grace", customer.FirstName);
        Assert.Equal("Hopper", customer.LastName);
        Assert.Equal(newAddress, customer.Address);
        Assert.Equal("Navy", customer.Company);
    }

    [Fact]
    public void UpdateFrom_WithValidData_UpdatesRequestedAmount()
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        customer.UpdateFrom("Ada", "Lovelace", ValidAddress(), "Acme", 9000m);

        // Assert
        Assert.Equal(9000m, customer.Application.RequestedAmount);
    }

    [Fact]
    public void UpdateFrom_WithValidData_KeepsIdsAndSsn()
    {
        // Arrange
        var customer = NewCustomer();
        var customerId = customer.Id;
        var applicationId = customer.Application.Id;

        // Act
        customer.UpdateFrom("Grace", "Hopper", ValidAddress(), "Navy", 9000m);

        // Assert
        Assert.Equal(customerId, customer.Id);
        Assert.Equal(applicationId, customer.Application.Id);
        Assert.Equal(customerId, customer.Application.CustomerId);
        Assert.Equal(ValidSsn, customer.Ssn);
    }

    [Fact]
    public void UpdateFrom_WithPaddedText_TrimsNamesAndCompany()
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        customer.UpdateFrom("  Grace ", " Hopper ", ValidAddress(), "  Navy  ", 2m);

        // Assert
        Assert.Equal("Grace", customer.FirstName);
        Assert.Equal("Hopper", customer.LastName);
        Assert.Equal("Navy", customer.Company);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFrom_WithBlankFirstName_ThrowsValidationErrorForFirstName(string? value)
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom(value, "Hopper", ValidAddress(), "Navy", 100m));

        // Assert
        Assert.Equal("firstName", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFrom_WithBlankLastName_ThrowsValidationErrorForLastName(string? value)
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom("Grace", value, ValidAddress(), "Navy", 100m));

        // Assert
        Assert.Equal("lastName", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateFrom_WithBlankCompany_ThrowsValidationErrorForCompany(string? value)
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom("Grace", "Hopper", ValidAddress(), value, 100m));

        // Assert
        Assert.Equal("company", ex.Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateFrom_WithNonPositiveAmount_ThrowsValidationErrorForRequestedAmount(double amount)
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom("Grace", "Hopper", ValidAddress(), "Navy", (decimal)amount));

        // Assert
        Assert.Equal("requestedAmount", ex.Field);
    }

    [Theory]
    [InlineData("100.001")]
    [InlineData("10000000000000000")]
    public void Create_WithUnstorableAmount_ThrowsValidationErrorForRequestedAmount(string amount)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            Customer.Create("Ada", "Lovelace", ValidSsn, ValidAddress(), "Acme", decimal.Parse(amount, CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("requestedAmount", ex.Field);
    }

    [Theory]
    [InlineData("100.001")]
    [InlineData("10000000000000000")]
    public void UpdateFrom_WithUnstorableAmount_ThrowsValidationErrorForRequestedAmount(string amount)
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        var ex = Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom("Grace", "Hopper", ValidAddress(), "Navy", decimal.Parse(amount, CultureInfo.InvariantCulture)));

        // Assert
        Assert.Equal("requestedAmount", ex.Field);
        Assert.Equal(5000m, customer.Application.RequestedAmount);
    }

    [Fact]
    public void Create_WithTwoDecimalAmount_AcceptsAmount()
    {
        // Act
        var customer = Customer.Create("Ada", "Lovelace", ValidSsn, ValidAddress(), "Acme", 100.10m);

        // Assert
        Assert.Equal(100.10m, customer.Application.RequestedAmount);
    }

    [Fact]
    public void UpdateFrom_WithTwoDecimalAmount_AcceptsAmount()
    {
        // Arrange
        var customer = NewCustomer();

        // Act
        customer.UpdateFrom("Grace", "Hopper", ValidAddress(), "Navy", 100.10m);

        // Assert
        Assert.Equal(100.10m, customer.Application.RequestedAmount);
    }

    [Fact]
    public void UpdateFrom_WithInvalidAmount_LeavesCustomerUnchanged()
    {
        // Arrange
        var customer = NewCustomer();
        var originalAddress = customer.Address;

        // Act
        Assert.Throws<DomainValidationException>(() =>
            customer.UpdateFrom("Grace", "Hopper", Address.Create("9 Oak Ave", "Dallas", "TX", "75001"), "Navy", 0m));

        // Assert
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal("Analytical Engines", customer.Company);
        Assert.Equal(originalAddress, customer.Address);
        Assert.Equal(5000m, customer.Application.RequestedAmount);
    }
}
