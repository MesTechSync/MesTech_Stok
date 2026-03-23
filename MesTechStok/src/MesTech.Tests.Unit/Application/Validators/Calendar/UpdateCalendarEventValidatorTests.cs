using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateCalendarEventValidatorTests
{
    private readonly UpdateCalendarEventValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new UpdateCalendarEventCommand(Guid.NewGuid(), IsCompleted: true);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_ShouldFail()
    {
        var cmd = new UpdateCalendarEventCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
