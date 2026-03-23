using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Onboarding;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CompleteOnboardingStepValidatorTests
{
    private readonly CompleteOnboardingStepValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CompleteOnboardingStepCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new CompleteOnboardingStepCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
