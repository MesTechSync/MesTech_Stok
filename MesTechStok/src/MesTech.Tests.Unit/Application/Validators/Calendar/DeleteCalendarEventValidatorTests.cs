using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteCalendarEventValidatorTests
{
    private readonly DeleteCalendarEventValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteCalendarEventCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_ShouldFail()
    {
        var cmd = new DeleteCalendarEventCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
