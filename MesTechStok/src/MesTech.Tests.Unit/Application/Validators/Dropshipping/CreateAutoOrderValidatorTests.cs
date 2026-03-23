using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateAutoOrderValidatorTests
{
    private readonly CreateAutoOrderValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SupplierId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    private static CreateAutoOrderCommand CreateValidCommand() => new(
        ProductIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
        SupplierId: Guid.NewGuid(),
        AutoApprove: false
    );
}
