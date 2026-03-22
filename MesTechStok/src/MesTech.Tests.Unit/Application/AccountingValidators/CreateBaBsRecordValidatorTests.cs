using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateBaBsRecordValidatorTests
{
    private readonly CreateBaBsRecordValidator _validator = new();

    private static CreateBaBsRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Year: 2026,
        Month: 3,
        Type: BaBsType.Ba,
        CounterpartyVkn: "1234567890",
        CounterpartyName: "ABC Ticaret Ltd.",
        TotalAmount: 25000m,
        DocumentCount: 5
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
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Theory]
    [InlineData(BaBsType.Ba)]
    [InlineData(BaBsType.Bs)]
    public async Task ValidType_PassesValidation(BaBsType type)
    {
        var cmd = ValidCommand() with { Type = type };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidType_FailsValidation()
    {
        var cmd = ValidCommand() with { Type = (BaBsType)99 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task EmptyCounterpartyVkn_FailsValidation()
    {
        var cmd = ValidCommand() with { CounterpartyVkn = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyVkn");
    }

    [Fact]
    public async Task CounterpartyVknTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { CounterpartyVkn = new string('V', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyVkn");
    }

    [Fact]
    public async Task EmptyCounterpartyName_FailsValidation()
    {
        var cmd = ValidCommand() with { CounterpartyName = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyName");
    }

    [Fact]
    public async Task CounterpartyNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { CounterpartyName = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyName");
    }

    [Fact]
    public async Task NegativeTotalAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { TotalAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalAmount");
    }

    [Fact]
    public async Task ZeroTotalAmount_PassesValidation()
    {
        var cmd = ValidCommand() with { TotalAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NegativeDocumentCount_FailsValidation()
    {
        var cmd = ValidCommand() with { DocumentCount = -1 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentCount");
    }

    [Fact]
    public async Task ZeroDocumentCount_PassesValidation()
    {
        var cmd = ValidCommand() with { DocumentCount = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
