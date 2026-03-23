using FluentAssertions;
using MesTech.Application.Commands.DeleteProduct;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Products;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteProductValidatorTests
{
    private readonly DeleteProductValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteProductCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductId_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteProductCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }
}
