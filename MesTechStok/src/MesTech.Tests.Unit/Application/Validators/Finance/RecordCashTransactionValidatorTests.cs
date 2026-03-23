using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Domain.Entities.Finance;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RecordCashTransactionValidatorTests
{
    private readonly RecordCashTransactionValidator _sut = new();

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
    public async Task ZeroAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task NegativeAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -100 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task EmptyDescription_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task DescriptionExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CategoryExceeds100Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Category = new string('C', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task CategoryNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Category = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidEnumType_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Type = (CashTransactionType)99 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Theory]
    [InlineData(CashTransactionType.Income)]
    [InlineData(CashTransactionType.Expense)]
    [InlineData(CashTransactionType.Transfer)]
    public async Task ValidType_ShouldPass(CashTransactionType type)
    {
        var cmd = CreateValidCommand() with { Type = type };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static RecordCashTransactionCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CashRegisterId: Guid.NewGuid(),
        Type: CashTransactionType.Income,
        Amount: 500m,
        Description: "Nakit satış tahsilatı"
    );
}
