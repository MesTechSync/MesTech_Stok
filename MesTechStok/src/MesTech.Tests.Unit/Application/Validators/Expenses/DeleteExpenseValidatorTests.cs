using FluentAssertions;
using MesTech.Application.Commands.DeleteExpense;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Expenses;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteExpenseValidatorTests
{
    private readonly DeleteExpenseValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteExpenseCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Id_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteExpenseCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
