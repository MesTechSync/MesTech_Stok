using FluentAssertions;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.AI;

public class GenerateProductDescriptionValidatorTests
{
    private readonly GenerateProductDescriptionValidator _sut = new();

    private static GenerateProductDescriptionCommand CreateValidCommand() => new(
        ProductId: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        ProductName: "Samsung Galaxy S24 Ultra 256GB",
        Category: "Elektronik",
        Brand: "Samsung",
        Features: new List<string> { "256GB", "5G", "S-Pen" },
        Language: "tr");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyProductId_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public async Task EmptyOrNullProductName_ShouldFail(string? productName)
    {
        var command = CreateValidCommand() with { ProductName = productName! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductName");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProductNameExceeds500Chars_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductName = new string('X', 501) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductName");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProductName500Chars_ShouldPass()
    {
        var command = CreateValidCommand() with { ProductName = new string('X', 500) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public async Task EmptyOrNullLanguage_ShouldFail(string? language)
    {
        var command = CreateValidCommand() with { Language = language! };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LanguageExceeds5Chars_ShouldFail()
    {
        var command = CreateValidCommand() with { Language = "trTRxx" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language");
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr-FR")]
    [Trait("Category", "Unit")]
    public async Task ValidLanguageCodes_ShouldPass(string language)
    {
        var command = CreateValidCommand() with { Language = language };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task NullCategoryAndBrand_ShouldPass()
    {
        var command = CreateValidCommand() with { Category = null, Brand = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task NullFeatures_ShouldPass()
    {
        var command = CreateValidCommand() with { Features = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MultipleInvalidFields_ShouldReturnMultipleErrors()
    {
        var command = CreateValidCommand() with
        {
            ProductId = Guid.Empty,
            TenantId = Guid.Empty,
            ProductName = "",
            Language = ""
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }
}
