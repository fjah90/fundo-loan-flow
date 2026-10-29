using LoanFlow.Domain;
using Xunit;

namespace LoanFlow.Tests.Domain;

public class SsnTests
{
    [Theory]
    [InlineData("123-45-6789")]
    [InlineData("123456789")]
    [InlineData("  123-45-6789  ")]
    [InlineData(" 123456789 ")]
    public void Parse_WithValidFormat_StoresNineDigits(string input)
    {
        // Act
        var ssn = Ssn.Parse(input);

        // Assert
        Assert.Equal("123456789", ssn.Value);
    }

    [Fact]
    public void Parse_WithDashedAndPlainForms_AreEqual()
    {
        // Act
        var dashed = Ssn.Parse("123-45-6789");
        var plain = Ssn.Parse("123456789");

        // Assert
        Assert.Equal(dashed, plain);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12-345-6789")]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("12345678a")]
    [InlineData("abc-de-fghi")]
    public void Parse_WithInvalidFormat_ThrowsValidationErrorForSsn(string? input)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => Ssn.Parse(input));

        // Assert
        Assert.Equal("ssn", ex.Field);
    }

    [Fact]
    public void ToString_WithValidSsn_MasksAllButLastFourDigits()
    {
        // Arrange
        var ssn = Ssn.Parse("123-45-6789");

        // Act
        var text = ssn.ToString();

        // Assert
        Assert.Equal("***-**-6789", text);
    }

    [Fact]
    public void ToString_WithValidSsn_DoesNotContainFullNumber()
    {
        // Arrange
        var ssn = Ssn.Parse("123456789");

        // Act
        var text = ssn.ToString();

        // Assert
        Assert.DoesNotContain("123456789", text);
        Assert.DoesNotContain("12345", text);
        Assert.DoesNotContain("123-45-6789", text);
    }
}
