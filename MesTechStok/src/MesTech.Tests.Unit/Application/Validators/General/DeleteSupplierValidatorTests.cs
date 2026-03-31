using FluentAssertions;
using MesTech.Application.Commands.DeleteSupplier;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteSupplierValidatorTests
{
    private readonly DeleteSupplierValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidCommand();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptySupplierId_ShouldFail()
    {
        var input = CreateValidCommand() with { SupplierId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    private static DeleteSupplierCommand CreateValidCommand() => new(SupplierId: Guid.NewGuid());
}
