using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UpdateChartOfAccountValidatorTests
{
    private readonly UpdateChartOfAccountValidator _validator = new();

    private static UpdateChartOfAccountCommand ValidCommand() =>
        new(Id: Guid.NewGuid(), Name: "Kasa Hesabi");

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

    [Fact]
    public async Task EmptyName_Should_Fail()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameTooLong_Should_Fail()
    {
        var cmd = ValidCommand() with { Name = new string('A', 201) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
