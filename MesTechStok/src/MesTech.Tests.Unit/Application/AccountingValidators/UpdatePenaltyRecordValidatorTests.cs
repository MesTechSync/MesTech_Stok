using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UpdatePenaltyRecordValidatorTests
{
    private readonly UpdatePenaltyRecordValidator _validator = new();

    private static UpdatePenaltyRecordCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        PaymentStatus: PaymentStatus.Completed
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

    [Fact]
    public async Task InvalidPaymentStatus_FailsValidation()
    {
        var cmd = ValidCommand() with { PaymentStatus = (PaymentStatus)99 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PaymentStatus");
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Cancelled)]
    public async Task ValidPaymentStatus_PassesValidation(PaymentStatus status)
    {
        var cmd = ValidCommand() with { PaymentStatus = status };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
