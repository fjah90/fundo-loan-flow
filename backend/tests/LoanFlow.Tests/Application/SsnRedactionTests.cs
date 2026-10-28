using LoanFlow.Application;
using LoanFlow.Domain;
using Xunit;

namespace LoanFlow.Tests.Application;

public sealed class SsnRedactionTests
{
    private const string Dashed = "123-45-6789";
    private const string Digits = "123456789";

    [Fact]
    public void SubmitLoanApplicationCommand_ToString_DoesNotContainSsn()
    {
        // Arrange
        var command = new SubmitLoanApplicationCommand(
            "Ada", "Lovelace", Dashed, "1 Main St", "Beverly Hills", "CA", "90210", "Analytical Engines", 5000m);

        // Act
        var text = command.ToString();

        // Assert
        Assert.DoesNotContain(Dashed, text);
        Assert.DoesNotContain(Digits, text);
    }

    [Fact]
    public void CustomerSyncRequested_ToString_DoesNotContainSsn()
    {
        // Arrange
        var customer = Customer.Create(
            "Ada", "Lovelace", Ssn.Parse(Dashed), Address.Create("1 Main St", "Beverly Hills", "CA", "90210"),
            "Analytical Engines", 5000m);
        var evt = CustomerSyncRequested.From(CustomerSyncKind.Created, customer);

        // Act
        var text = evt.ToString();

        // Assert
        Assert.DoesNotContain(Dashed, text);
        Assert.DoesNotContain(Digits, text);
    }
}
