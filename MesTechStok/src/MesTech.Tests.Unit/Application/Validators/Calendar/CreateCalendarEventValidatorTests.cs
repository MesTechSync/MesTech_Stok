using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCalendarEventValidatorTests
{
    private readonly CreateCalendarEventValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyTitle_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleExceeds300Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = new string('T', 301) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task InvalidEventType_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Type = (EventType)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task EndBeforeStart_NonAllDay_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            IsAllDay = false,
            StartAt = DateTime.UtcNow.AddHours(2),
            EndAt = DateTime.UtcNow.AddHours(1)
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EndBeforeStart_AllDay_ShouldPass()
    {
        var cmd = CreateValidCommand() with
        {
            IsAllDay = true,
            StartAt = DateTime.UtcNow.AddHours(2),
            EndAt = DateTime.UtcNow.AddHours(1)
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionExceeds2000Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 2001) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task DescriptionNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task LocationExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Location = new string('L', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Location");
    }

    [Fact]
    public async Task LocationNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Location = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateCalendarEventCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "KDV Beyanname Son Gun",
        StartAt: DateTime.UtcNow.AddDays(1),
        EndAt: DateTime.UtcNow.AddDays(1).AddHours(1),
        Type: EventType.Custom,
        Description: "Aylik KDV beyannamesi",
        Location: "Ofis"
    );
}
