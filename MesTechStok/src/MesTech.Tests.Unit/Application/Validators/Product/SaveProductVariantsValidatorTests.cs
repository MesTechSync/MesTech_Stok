using FluentAssertions;
using MesTech.Application.Features.Product.Commands.SaveProductVariants;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
public class SaveProductVariantsValidatorTests
{
    private readonly SaveProductVariantsValidator _sut = new();

    private static SaveProductVariantsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        Variants: new List<ProductVariantInput>
        {
            new ProductVariantInput { SKU = "SKU-001", Color = "Red", Size = "M", Price = 149.99m, Stock = 50, Barcode = "8680001000001" },
            new ProductVariantInput { SKU = "SKU-002", Color = "Blue", Size = "L", Price = 159.99m, Stock = 30, Barcode = "8680001000002" }
        });

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyProductId_ShouldFail()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task EmptyVariantsList_ShouldFail()
    {
        var command = CreateValidCommand() with { Variants = new List<ProductVariantInput>() };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Variants");
    }

    [Fact]
    public async Task NullVariantsList_ShouldFail()
    {
        var command = CreateValidCommand() with { Variants = null! };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Variants");
    }

    [Fact]
    public async Task Variant_EmptySKU_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "", Color = "Red", Size = "M", Price = 100m, Stock = 10, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_SKU_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = new string('S', 101), Color = null, Size = null, Price = 100m, Stock = 10, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_NegativePrice_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-NEG", Color = null, Size = null, Price = -1m, Stock = 10, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_NegativeStock_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-STOCK", Color = null, Size = null, Price = 100m, Stock = -5, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_Color_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-CLR", Color = new string('C', 101), Size = null, Price = 100m, Stock = 10, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_Size_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-SZ", Color = null, Size = new string('Z', 51), Price = 100m, Stock = 10, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_Barcode_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-BC", Color = null, Size = null, Price = 100m, Stock = 10, Barcode = new string('B', 51) }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Variant_NullOptionalFields_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-MIN", Color = null, Size = null, Price = 0m, Stock = 0, Barcode = null }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Variant_ZeroPriceAndStock_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = "SKU-ZERO", Color = "White", Size = "S", Price = 0m, Stock = 0, Barcode = "1234567890123" }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Variant_MaxLengthFields_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<ProductVariantInput>
            {
                new ProductVariantInput { SKU = new string('S', 100), Color = new string('C', 100), Size = new string('Z', 50),
                    Price = 99999m, Stock = 999999, Barcode = new string('B', 50) }
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
