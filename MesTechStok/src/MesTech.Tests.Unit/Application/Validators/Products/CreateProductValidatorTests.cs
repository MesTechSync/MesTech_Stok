using FluentAssertions;
using MesTech.Application.Commands.CreateProduct;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task SKU_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task SKU_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SKU = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public async Task Barcode_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Barcode = new string('B', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public async Task PurchasePrice_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PurchasePrice = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchasePrice");
    }

    [Fact]
    public async Task SalePrice_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SalePrice = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SalePrice");
    }

    [Fact]
    public async Task CategoryId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CategoryId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public async Task Description_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task MinimumStock_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MinimumStock = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinimumStock");
    }

    [Fact]
    public async Task MaximumStock_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MaximumStock = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaximumStock");
    }

    [Fact]
    public async Task TaxRate_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TaxRate = -0.01m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public async Task Brand_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Brand = new string('B', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Brand");
    }

    [Fact]
    public async Task ImageUrl_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ImageUrl = new string('U', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    private static CreateProductCommand CreateValidCommand() => new(
        Name: "Test Product",
        SKU: "SKU-001",
        Barcode: "1234567890",
        PurchasePrice: 10.00m,
        SalePrice: 20.00m,
        CategoryId: Guid.NewGuid(),
        Description: "Test description",
        MinimumStock: 5,
        MaximumStock: 1000,
        TaxRate: 0.18m,
        Brand: "TestBrand",
        ImageUrl: "https://example.com/img.jpg"
    );
}
