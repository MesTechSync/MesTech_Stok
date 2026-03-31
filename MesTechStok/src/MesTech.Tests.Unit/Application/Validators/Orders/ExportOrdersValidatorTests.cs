using FluentAssertions;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
public class ExportOrdersValidatorTests
{
    private readonly ExportOrdersValidator _sut = new();

    private static ExportOrdersCommand CreateValidCommand() =>
        new(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, null);

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task From_WhenAfterTo_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            From = DateTime.UtcNow,
            To = DateTime.UtcNow.AddDays(-1)
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "From");
    }

    [Fact]
    public async Task From_WhenEqualToTo_ShouldFail()
    {
        var now = DateTime.UtcNow;
        var command = CreateValidCommand() with { From = now, To = now };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task To_WhenFarFuture_ShouldFail()
    {
        var command = CreateValidCommand() with { To = DateTime.UtcNow.AddDays(30) };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "To");
    }

    [Fact]
    public async Task To_WhenTomorrow_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow.AddHours(23)
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformFilter_WhenNull_ShouldPass()
    {
        var command = CreateValidCommand() with { PlatformFilter = null };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformFilter_WhenProvided_ShouldPass()
    {
        var command = CreateValidCommand() with { PlatformFilter = "Trendyol" };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task From_WhenBeforeTo_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            From = DateTime.UtcNow.AddDays(-7),
            To = DateTime.UtcNow
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenValid_ShouldPass()
    {
        var command = CreateValidCommand() with { TenantId = Guid.NewGuid() };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleFields_WhenInvalid_ShouldFailWithMultipleErrors()
    {
        var command = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            From = DateTime.UtcNow,
            To = DateTime.UtcNow.AddDays(-1)
        };
        var result = await _sut.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
