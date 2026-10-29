using System.ComponentModel.DataAnnotations;
using LoanFlow.Api;
using Xunit;

namespace LoanFlow.Tests.Application;

public sealed class FreeTextAttributeTests
{
    private static ValidationResult? Validate(string? value) =>
        new FreeTextAttribute().GetValidationResult(value, new ValidationContext(new object()) { MemberName = "City" });

    [Fact]
    public void FreeText_ExactlyMaxLength_IsValid()
    {
        // Arrange
        var value = new string('a', 100);

        // Act
        var result = Validate(value);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void FreeText_OverMaxLength_ReturnsLengthMessage()
    {
        // Arrange
        var value = new string('a', 101);

        // Act
        var result = Validate(value);

        // Assert
        Assert.Equal("Must be 100 characters or fewer.", result?.ErrorMessage);
        Assert.Equal(["City"], result?.MemberNames);
    }

    [Fact]
    public void FreeText_ContainsNul_ReturnsInvalidCharacterMessage()
    {
        // Arrange
        var value = "Be\0verly";

        // Act
        var result = Validate(value);

        // Assert
        Assert.Equal("Contains an invalid character.", result?.ErrorMessage);
    }

    [Fact]
    public void FreeText_Null_IsValid()
    {
        // Act
        var result = Validate(null);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }
}
