using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.SaveCrmSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
public class SaveCrmSettingsValidatorTests
{
    private readonly SaveCrmSettingsValidator _sut = new();

    private static SaveCrmSettingsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        AutoAssignLeads: true,
        DefaultPipelineId: Guid.NewGuid(),
        LeadScoreThreshold: 50,
        EnableEmailTracking: true);

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task LeadScoreThreshold_AtZero_ShouldPass()
    {
        var command = CreateValidCommand() with { LeadScoreThreshold = 0 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LeadScoreThreshold_At100_ShouldPass()
    {
        var command = CreateValidCommand() with { LeadScoreThreshold = 100 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LeadScoreThreshold_Below0_ShouldFail()
    {
        var command = CreateValidCommand() with { LeadScoreThreshold = -1 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeadScoreThreshold");
    }

    [Fact]
    public async Task LeadScoreThreshold_Above100_ShouldFail()
    {
        var command = CreateValidCommand() with { LeadScoreThreshold = 101 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeadScoreThreshold");
    }

    [Fact]
    public async Task NullDefaultPipelineId_ShouldPass()
    {
        var command = CreateValidCommand() with { DefaultPipelineId = null };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AutoAssignLeads_False_ShouldPass()
    {
        var command = CreateValidCommand() with { AutoAssignLeads = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EnableEmailTracking_False_ShouldPass()
    {
        var command = CreateValidCommand() with { EnableEmailTracking = false };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(150)]
    [InlineData(999)]
    public async Task LeadScoreThreshold_OutOfRange_ShouldFail(int threshold)
    {
        var command = CreateValidCommand() with { LeadScoreThreshold = threshold };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeadScoreThreshold");
    }
}
