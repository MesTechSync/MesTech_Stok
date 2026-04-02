using FluentAssertions;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Commands.DeleteProduct;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ProductValidatorTests
{
    // ═══ CreateProduct ═══

    private static CreateProductCommand ValidCreateProduct() => new(
        Name: "iPhone 15 Pro", SKU: "IP15P-128", Barcode: "8680000000001",
        PurchasePrice: 45000m, SalePrice: 55000m, CategoryId: Guid.NewGuid());

    [Fact]
    public void CreateProduct_Valid_Passes()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProduct_EmptyName_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { Name = "" }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_EmptySKU_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { SKU = "" }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_NameOver500_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { Name = new string('A', 501) }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_NegativePurchasePrice_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { PurchasePrice = -1m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_NegativeSalePrice_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { SalePrice = -1m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_EmptyCategoryId_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { CategoryId = Guid.Empty }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_NegativeMinStock_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { MinimumStock = -1 }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_NegativeTaxRate_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { TaxRate = -0.01m }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_BarcodeOver500_Fails()
    {
        var v = new CreateProductValidator();
        v.Validate(ValidCreateProduct() with { Barcode = new string('B', 501) }).IsValid.Should().BeFalse();
    }

    // ═══ UpdateProduct ═══

    [Fact]
    public void UpdateProduct_Valid_Passes()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), Name: "Updated")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProduct_EmptyId_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProduct_DescriptionOver2000_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), Description: new string('D', 2001))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProduct_NegativePurchasePrice_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), PurchasePrice: -1m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProduct_ZeroSalePrice_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), SalePrice: 0m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProduct_TaxRateOver1_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), TaxRate: 1.5m)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateProduct_BrandOver200_Fails()
    {
        var v = new UpdateProductValidator();
        v.Validate(new UpdateProductCommand(Guid.NewGuid(), Brand: new string('B', 201))).IsValid.Should().BeFalse();
    }

    // ═══ DeleteProduct ═══

    [Fact]
    public void DeleteProduct_Valid_Passes()
    {
        var v = new DeleteProductValidator();
        v.Validate(new DeleteProductCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteProduct_EmptyId_Fails()
    {
        var v = new DeleteProductValidator();
        v.Validate(new DeleteProductCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
