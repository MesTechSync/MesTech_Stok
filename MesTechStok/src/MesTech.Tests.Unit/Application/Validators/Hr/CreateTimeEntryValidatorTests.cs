using FluentAssertions;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Hr;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateTimeEntryValidatorTests
{
    private readonly CreateTimeEntryValidator _sut = new();

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
    public async Task EmptyWorkTaskId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { WorkTaskId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkTaskId");
    }

    [Fact]
    public async Task EmptyUserId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public async Task NullDescription_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionExceeds500_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task DescriptionExactly500_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HourlyRate_WhenNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { HourlyRate = -1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HourlyRate");
    }

    [Fact]
    public async Task HourlyRate_WhenPositive_ShouldPass()
    {
        var cmd = CreateValidCommand() with { HourlyRate = 150m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HourlyRate_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { HourlyRate = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task IsBillable_False_ShouldPass()
    {
        var cmd = CreateValidCommand() with { IsBillable = false };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleInvalidFields_ShouldReportAll()
    {
        var cmd = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            WorkTaskId = Guid.Empty,
            UserId = Guid.Empty
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
    }

    private static CreateTimeEntryCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        WorkTaskId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        Description: "Backend API gelistirme",
        IsBillable: true,
        HourlyRate: 200m);
}
