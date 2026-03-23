using FluentAssertions;
using MesTech.Application.Commands.CreateEInvoiceFromDraft;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateEInvoiceFromDraftValidatorTests
{
    private readonly CreateEInvoiceFromDraftValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task OrderId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task SuggestedEttnNo_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { SuggestedEttnNo = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuggestedEttnNo");
    }

    [Fact]
    public async Task SuggestedEttnNo_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { SuggestedEttnNo = new string('E', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuggestedEttnNo");
    }

    [Fact]
    public async Task SuggestedEttnNo_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { SuggestedEttnNo = new string('E', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand();
        cmd = cmd with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static CreateEInvoiceFromDraftCommand CreateValidCommand() => new()
    {
        OrderId = Guid.NewGuid(),
        SuggestedEttnNo = "ETTN-2026-001",
        SuggestedTotal = 1500m,
        TenantId = Guid.NewGuid()
    };
}
