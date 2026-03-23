using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeactivateCampaignCommandValidatorTests
{
    private readonly DeactivateCampaignCommandValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeactivateCampaignCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCampaignId_ShouldFail()
    {
        var cmd = new DeactivateCampaignCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CampaignId");
    }
}
