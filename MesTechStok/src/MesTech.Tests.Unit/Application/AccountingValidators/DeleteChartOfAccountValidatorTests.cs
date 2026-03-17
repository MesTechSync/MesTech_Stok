using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class DeleteChartOfAccountValidatorTests
{
    private readonly DeleteChartOfAccountValidator _validator = new();

    private static DeleteChartOfAccountCommand ValidCommand() =>
        new(Id: Guid.NewGuid(), DeletedBy: "admin");

    [Fact]
    public async Task Valid_Input_Should_Pass()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_Should_Fail()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
