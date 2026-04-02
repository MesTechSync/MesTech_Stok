using FluentAssertions;
using MesTech.Application.Commands.CreateProduct;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    private static CreateProductCommand ValidCommand() => new(
        Name: "Test Ürün",
        SKU: "SKU-001",
        Barcode: "8680001234567",
        PurchasePrice: 50m,
        SalePrice: 100m,
        CategoryId: Guid.NewGuid(),
        Description: "Test açıklama",
        MinimumStock: 5,
        MaximumStock: 1000,
        TaxRate: 0.18m,
        Brand: "TestBrand",
        ImageUrl: "https://example.com/image.jpg");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over500_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('X', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_SKU_Fails()
    {
        var cmd = ValidCommand() with { SKU = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void SKU_Over500_Fails()
    {
        var cmd = ValidCommand() with { SKU = new string('S', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void Negative_PurchasePrice_Fails()
    {
        var cmd = ValidCommand() with { PurchasePrice = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchasePrice");
    }

    [Fact]
    public void Negative_SalePrice_Fails()
    {
        var cmd = ValidCommand() with { SalePrice = -0.01m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SalePrice");
    }

    [Fact]
    public void Empty_CategoryId_Fails()
    {
        var cmd = ValidCommand() with { CategoryId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Negative_MinimumStock_Fails()
    {
        var cmd = ValidCommand() with { MinimumStock = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinimumStock");
    }

    [Fact]
    public void Negative_MaximumStock_Fails()
    {
        var cmd = ValidCommand() with { MaximumStock = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaximumStock");
    }

    [Fact]
    public void Negative_TaxRate_Fails()
    {
        var cmd = ValidCommand() with { TaxRate = -0.01m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public void Barcode_Over500_Fails()
    {
        var cmd = ValidCommand() with { Barcode = new string('B', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Barcode_Null_Passes()
    {
        var cmd = ValidCommand() with { Barcode = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Description_Over500_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Brand_Over500_Fails()
    {
        var cmd = ValidCommand() with { Brand = new string('B', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Brand");
    }

    [Fact]
    public void ImageUrl_Over500_Fails()
    {
        var cmd = ValidCommand() with { ImageUrl = new string('I', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }

    [Fact]
    public void Zero_PurchasePrice_Passes()
    {
        var cmd = ValidCommand() with { PurchasePrice = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "PurchasePrice");
    }

    [Fact]
    public void Zero_SalePrice_Passes()
    {
        var cmd = ValidCommand() with { SalePrice = 0m };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "SalePrice");
    }
}
