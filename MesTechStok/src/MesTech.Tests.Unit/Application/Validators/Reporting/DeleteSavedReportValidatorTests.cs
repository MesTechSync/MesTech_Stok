using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;

namespace MesTech.Tests.Unit.Application.Validators.Reporting;

[Trait("Category", "Unit")]
[Trait("Feature", "Reporting")]
public class DeleteSavedReportValidatorTests
{
    private readonly DeleteSavedReportValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var cmd = new DeleteSavedReportCommand(TenantId: Guid.NewGuid(), ReportId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var cmd = new DeleteSavedReportCommand(TenantId: Guid.Empty, ReportId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyReportId_Fails()
    {
        var cmd = new DeleteSavedReportCommand(TenantId: Guid.NewGuid(), ReportId: Guid.Empty);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
