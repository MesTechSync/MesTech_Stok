using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GenerateTaxCalendarValidatorTests
{
    private readonly GenerateTaxCalendarValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new GenerateTaxCalendarCommand(2026, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new GenerateTaxCalendarCommand(2026, Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
