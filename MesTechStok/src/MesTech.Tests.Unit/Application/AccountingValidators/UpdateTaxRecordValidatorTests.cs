using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UpdateTaxRecordValidatorTests
{
    private readonly UpdateTaxRecordValidator _validator = new();

    private static UpdateTaxRecordCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        MarkAsPaid: true
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
