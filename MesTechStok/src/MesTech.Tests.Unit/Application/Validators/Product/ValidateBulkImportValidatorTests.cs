using System.IO;
using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ValidateBulkImportValidatorTests
{
    private readonly ValidateBulkImportValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ValidateBulkImportCommand(Stream.Null, "products.xlsx");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFileName_ShouldFail()
    {
        var cmd = new ValidateBulkImportCommand(Stream.Null, "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task FileNameExceeds500Chars_ShouldFail()
    {
        var cmd = new ValidateBulkImportCommand(Stream.Null, new string('V', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }
}
