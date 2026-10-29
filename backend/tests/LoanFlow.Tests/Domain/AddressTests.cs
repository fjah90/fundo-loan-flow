using LoanFlow.Domain;
using Xunit;

namespace LoanFlow.Tests.Domain;

public class UsStateTests
{
    [Theory]
    [InlineData("NY", "NY")]
    [InlineData("ny", "NY")]
    [InlineData(" ny ", "NY")]
    [InlineData("Ca", "CA")]
    [InlineData("DC", "DC")]
    [InlineData("AK", "AK")]
    [InlineData("WY", "WY")]
    public void Parse_WithValidCode_NormalizesToUpperCase(string input, string expected)
    {
        // Act
        var state = UsState.Parse(input);

        // Assert
        Assert.Equal(expected, state.Code);
    }

    [Fact]
    public void Parse_WithDifferentCasing_AreEqual()
    {
        // Act
        var lower = UsState.Parse("tx");
        var upper = UsState.Parse("TX");

        // Assert
        Assert.Equal(lower, upper);
    }

    [Fact]
    public void ToString_WithValidState_ReturnsCode()
    {
        // Arrange
        var state = UsState.Parse("fl");

        // Act
        var text = state.ToString();

        // Assert
        Assert.Equal("FL", text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("XX")]
    [InlineData("PR")]
    [InlineData("New York")]
    public void Parse_WithInvalidCode_ThrowsValidationErrorForState(string? input)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => UsState.Parse(input));

        // Assert
        Assert.Equal("state", ex.Field);
    }
}

public class AddressTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        // Act
        var address = Address.Create("1 Main St", "Albany", "ny", "12345");

        // Assert
        Assert.Equal("1 Main St", address.Street);
        Assert.Equal("Albany", address.City);
        Assert.Equal(UsState.Parse("NY"), address.State);
        Assert.Equal("12345", address.ZipCode);
    }

    [Fact]
    public void Create_WithPaddedStreetAndCity_TrimsBoth()
    {
        // Act
        var address = Address.Create("  1 Main St  ", "  Albany ", "NY", "12345");

        // Assert
        Assert.Equal("1 Main St", address.Street);
        Assert.Equal("Albany", address.City);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345-6789")]
    public void Create_WithValidZip_KeepsZip(string zip)
    {
        // Act
        var address = Address.Create("1 Main St", "Albany", "NY", zip);

        // Assert
        Assert.Equal(zip, address.ZipCode);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("12345-67")]
    [InlineData("abcde")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithInvalidZip_ThrowsValidationErrorForZipCode(string? zip)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create("1 Main St", "Albany", "NY", zip));

        // Assert
        Assert.Equal("zipCode", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankStreet_ThrowsValidationErrorForStreet(string? street)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create(street, "Albany", "NY", "12345"));

        // Assert
        Assert.Equal("street", ex.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankCity_ThrowsValidationErrorForCity(string? city)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create("1 Main St", city, "NY", "12345"));

        // Assert
        Assert.Equal("city", ex.Field);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_WithInvalidState_ThrowsValidationErrorForState(string? state)
    {
        // Act
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create("1 Main St", "Albany", state, "12345"));

        // Assert
        Assert.Equal("state", ex.Field);
    }
}
