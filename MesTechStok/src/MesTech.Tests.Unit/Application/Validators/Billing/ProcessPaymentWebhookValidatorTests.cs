using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ProcessPaymentWebhookValidatorTests
{
    private readonly ProcessPaymentWebhookValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ProcessPaymentWebhookCommand("stripe", "{\"type\":\"payment\"}", "sig_abc");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProvider_ShouldFail()
    {
        var cmd = new ProcessPaymentWebhookCommand("", "{}", "sig");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public async Task ProviderExceeds100_ShouldFail()
    {
        var cmd = new ProcessPaymentWebhookCommand(new string('P', 101), "{}", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public async Task EmptyRawBody_ShouldFail()
    {
        var cmd = new ProcessPaymentWebhookCommand("stripe", "", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RawBody");
    }

    [Fact]
    public async Task NullSignature_ShouldPass()
    {
        var cmd = new ProcessPaymentWebhookCommand("iyzico", "{\"ok\":true}", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
