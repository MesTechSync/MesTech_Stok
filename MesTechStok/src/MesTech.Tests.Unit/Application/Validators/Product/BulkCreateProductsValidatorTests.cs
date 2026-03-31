using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkCreateProducts;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
public class BulkCreateProductsValidatorTests
{
    private readonly BulkCreateProductsValidator _sut = new();

    private static BulkCreateProductsCommand CreateValidCommand() =>
        new(Guid.NewGuid(), new List<BulkProductInput> { new() { Name = "Test" } }.AsReadOnly());

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Products_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { Products = new List<BulkProductInput>().AsReadOnly() };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Products");
    }

    [Fact]
    public async Task Products_WhenSingleItem_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Products = new List<BulkProductInput> { new() { Name = "Single" } }.AsReadOnly()
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Products_WhenExactly1000_ShouldPass()
    {
        var items = Enumerable.Range(1, 1000)
            .Select(i => new BulkProductInput { Name = $"Product{i}" })
            .ToList()
            .AsReadOnly();
        var command = CreateValidCommand() with { Products = items };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Products_WhenExceeds1000_ShouldFail()
    {
        var items = Enumerable.Range(1, 1001)
            .Select(i => new BulkProductInput { Name = $"Product{i}" })
            .ToList()
            .AsReadOnly();
        var command = CreateValidCommand() with { Products = items };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Products");
    }

    [Fact]
    public async Task Products_WhenMultipleItems_ShouldPass()
    {
        var items = new List<BulkProductInput>
        {
            new() { Name = "Product A" },
            new() { Name = "Product B" },
            new() { Name = "Product C" }
        }.AsReadOnly();
        var command = CreateValidCommand() with { Products = items };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty, Products = new List<BulkProductInput>().AsReadOnly() };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
