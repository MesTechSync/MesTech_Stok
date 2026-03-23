using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class MarkExpensePaidValidatorTests
{
    private readonly MarkExpensePaidValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyExpenseId_ShouldFail()
    {
        var cmd = new MarkExpensePaidCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpenseId");
    }

    [Fact]
    public async Task EmptyBankAccountId_ShouldFail()
    {
        var cmd = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankAccountId");
    }
}
