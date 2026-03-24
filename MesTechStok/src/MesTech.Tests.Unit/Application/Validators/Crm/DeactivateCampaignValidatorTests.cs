using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Crm")]
public class DeactivateCampaignValidatorTests
{
    private readonly DeactivateCampaignValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new DeactivateCampaignCommand(CampaignId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCampaignId_Fails()
    {
        var cmd = new DeactivateCampaignCommand(CampaignId: Guid.Empty);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
