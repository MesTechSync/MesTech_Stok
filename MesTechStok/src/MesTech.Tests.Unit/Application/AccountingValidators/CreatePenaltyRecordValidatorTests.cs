using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreatePenaltyRecordValidatorTests
{
    private readonly CreatePenaltyRecordValidator _validator = new();

    private static CreatePenaltyRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Source: PenaltySource.Trendyol,
        Description: "Gec teslimat cezasi",
        Amount: 250m,
        PenaltyDate: new DateTime(2026, 3, 1),
        DueDate: new DateTime(2026, 3, 15),
        ReferenceNumber: "PEN-2026-001",
        RelatedOrderId: Guid.NewGuid(),
        Currency: "TRY",
        Notes: null
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyDescription_FailsValidation()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DescriptionTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { Amount = -50m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CurrencyTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "USDX" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCurrency_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ReferenceNumberTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { ReferenceNumber = new string('R', 101) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NotesTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DueDateBeforePenaltyDate_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            PenaltyDate = new DateTime(2026, 3, 15),
            DueDate = new DateTime(2026, 3, 1)
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DueDateEqualsPenaltyDate_PassesValidation()
    {
        var date = new DateTime(2026, 3, 15);
        var cmd = ValidCommand() with
        {
            PenaltyDate = date,
            DueDate = date
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullDueDate_PassesValidation()
    {
        var cmd = ValidCommand() with { DueDate = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
