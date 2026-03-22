using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class DeleteSalaryRecordValidatorTests
{
    private readonly DeleteSalaryRecordValidator _validator = new();

    private static DeleteSalaryRecordCommand ValidCommand() => new(
        Id: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_FailsValidation()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
