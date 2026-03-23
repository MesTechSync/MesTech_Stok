using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class WinDealValidatorTests
{
    private readonly WinDealValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new WinDealCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyDealId_ShouldFail()
    {
        var cmd = new WinDealCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DealId");
    }
}
