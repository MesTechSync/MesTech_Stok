using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UpdateSalaryRecordValidatorTests
{
    private readonly UpdateSalaryRecordValidator _validator = new();

    private static UpdateSalaryRecordCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        PaymentStatus: PaymentStatus.Completed,
        PaidDate: new DateTime(2026, 3, 15)
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
    [InlineData(PaymentStatus.Scheduled)]
    [InlineData(PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Completed)]
    public async Task ValidPaymentStatus_PassesValidation(PaymentStatus status)
    {
        var cmd = ValidCommand() with { PaymentStatus = status };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
