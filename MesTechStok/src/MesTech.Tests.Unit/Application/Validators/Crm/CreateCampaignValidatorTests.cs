using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Crm")]
public class CreateCampaignValidatorTests
{
    private readonly CreateCampaignValidator _validator = new();

    private static CreateCampaignCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Yaz Kampanyasi",
        StartDate: new DateTime(2026, 6, 1),
        EndDate: new DateTime(2026, 6, 30),
        DiscountPercent: 15m,
        PlatformType: null,
        ProductIds: null);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyName_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NameTooLong_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('K', 301) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DiscountZero_Fails()
    {
        var cmd = ValidCommand() with { DiscountPercent = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DiscountOver100_Fails()
    {
        var cmd = ValidCommand() with { DiscountPercent = 101m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EndDateBeforeStartDate_Fails()
    {
        var cmd = ValidCommand() with { EndDate = new DateTime(2026, 5, 1) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
