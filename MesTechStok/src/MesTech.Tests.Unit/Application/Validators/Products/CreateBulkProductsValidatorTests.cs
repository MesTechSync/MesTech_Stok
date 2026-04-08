using FluentAssertions;
using MesTech.Application.Commands.CreateBulkProducts;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateBulkProductsValidatorTests
{
    private readonly CreateBulkProductsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Count_WhenNegative_ShouldFail()
    {
        var cmd = new CreateBulkProductsCommand(-1);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Count");
    }

    [Fact]
    public async Task Count_WhenZero_ShouldFail()
    {
        var cmd = new CreateBulkProductsCommand(0);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Count");
    }

    private static CreateBulkProductsCommand CreateValidCommand() => new(40);
}
