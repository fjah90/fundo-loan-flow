using LoanFlow.Api;
using Xunit;

namespace LoanFlow.Tests.Application;

public sealed class LoanApplicationRequestTests
{
    private const string Dashed = "123-45-6789";
    private const string Digits = "123456789";

    [Fact]
    public void LoanApplicationRequest_ToString_DoesNotContainSsn()
    {
        // Arrange
        var request = new LoanApplicationRequest(
            "Ada", "Lovelace", Dashed, "1 Main St", "Beverly Hills", "CA", "90210", "Analytical Engines", 5000m);

        // Act
        var text = request.ToString();

        // Assert
        Assert.DoesNotContain(Dashed, text);
        Assert.DoesNotContain(Digits, text);
        Assert.Contains("Ada", text);
    }
}
