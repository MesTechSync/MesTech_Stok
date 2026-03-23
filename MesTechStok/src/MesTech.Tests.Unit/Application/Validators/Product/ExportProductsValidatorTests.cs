using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ExportProductsValidatorTests
{
    private readonly ExportProductsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ExportProductsCommand(Format: "xlsx");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFormat_ShouldFail()
    {
        var cmd = new ExportProductsCommand(Format: "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task FormatExceeds500Chars_ShouldFail()
    {
        var cmd = new ExportProductsCommand(Format: new string('X', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }
}
