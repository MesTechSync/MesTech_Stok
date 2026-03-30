using FluentAssertions;
using MesTech.Application.Commands.CreateReconciliationTask;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateReconciliationTaskValidatorTests
{
    private readonly CreateReconciliationTaskValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateReconciliationTaskCommand
        {
            TenantId = Guid.NewGuid(),
            SettlementBatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            Confidence = 0.95m,
            Rationale = "Tutar ve tarih eşleşmesi"
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CreateReconciliationTaskCommand
        {
            TenantId = Guid.Empty,
            SettlementBatchId = Guid.NewGuid(),
            BankTransactionId = Guid.NewGuid(),
            Confidence = 0.80m
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = new CreateReconciliationTaskCommand
        {
            TenantId = Guid.NewGuid(),
            SettlementBatchId = null,
            BankTransactionId = null,
            Confidence = 0.50m,
            Rationale = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
