using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Finance;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApproveExpenseValidatorTests
{
    private readonly ApproveExpenseValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ApproveExpenseCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyExpenseId_ShouldFail()
    {
        var cmd = new ApproveExpenseCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpenseId");
    }

    [Fact]
    public async Task EmptyApproverUserId_ShouldFail()
    {
        var cmd = new ApproveExpenseCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApproverUserId");
    }
}
