using FluentAssertions;
using MesTech.Application.Commands.CreateReconciliationTask;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateReconciliationTaskValidatorTests
{
    private readonly CreateReconciliationTaskValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static CreateReconciliationTaskCommand CreateValidCommand() => new()
    {
        SettlementBatchId = Guid.NewGuid(),
        BankTransactionId = Guid.NewGuid(),
        Confidence = 0.95m,
        Rationale = "Auto-matched by settlement engine",
        TenantId = Guid.NewGuid()
    };
}
