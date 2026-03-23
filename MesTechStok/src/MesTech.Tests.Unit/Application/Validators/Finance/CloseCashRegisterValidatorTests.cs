using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CloseCashRegisterValidatorTests
{
    private readonly CloseCashRegisterValidator _sut = new();

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
    public async Task EmptyCashRegisterId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CashRegisterId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CashRegisterId");
    }

    [Fact]
    public async Task DefaultClosingDate_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ClosingDate = default };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClosingDate");
    }

    [Fact]
    public async Task NegativeActualCashAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ActualCashAmount = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ActualCashAmount");
    }

    [Fact]
    public async Task ZeroActualCashAmount_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ActualCashAmount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CloseCashRegisterCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CashRegisterId: Guid.NewGuid(),
        ClosingDate: DateTime.UtcNow,
        ActualCashAmount: 5000m
    );
}
