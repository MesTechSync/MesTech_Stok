using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;

namespace MesTech.Tests.Unit.Application.Validators.Reporting;

[Trait("Category", "Unit")]
[Trait("Feature", "Reporting")]
public class CreateSavedReportValidatorTests
{
    private readonly CreateSavedReportValidator _validator = new();

    private static CreateSavedReportCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Monthly Sales",
        ReportType: "SalesReport",
        FilterJson: "{\"month\":3}",
        CreatedByUserId: Guid.NewGuid());

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
        var cmd = ValidCommand() with { Name = new string('N', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyReportType_Fails()
    {
        var cmd = ValidCommand() with { ReportType = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyFilterJson_Fails()
    {
        var cmd = ValidCommand() with { FilterJson = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task FilterJsonTooLong_Fails()
    {
        var cmd = ValidCommand() with { FilterJson = new string('J', 4001) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCreatedByUserId_Fails()
    {
        var cmd = ValidCommand() with { CreatedByUserId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
