using FluentAssertions;
using MesTech.Application.Commands.FinalizeErpReconciliation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class FinalizeErpReconciliationValidatorTests
{
    private readonly FinalizeErpReconciliationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpProvider = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    public async Task ErpProvider_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpProvider = new string('P', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static FinalizeErpReconciliationCommand CreateValidCommand() => new()
    {
        ErpProvider = "Parasut",
        ReconciledCount = 42,
        MismatchCount = 3,
        TenantId = Guid.NewGuid()
    };
}
