using FluentAssertions;
using MesTech.Application.Commands.DeleteIncome;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Income;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteIncomeValidatorTests
{
    private readonly DeleteIncomeValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeleteIncomeCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Id_WhenEmpty_ShouldFail()
    {
        var cmd = new DeleteIncomeCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
