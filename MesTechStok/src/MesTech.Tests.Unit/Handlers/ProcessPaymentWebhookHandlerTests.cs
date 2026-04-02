using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
[Trait("Layer", "Billing")]
public class ProcessPaymentWebhookHandlerTests
{
    private readonly Mock<ITenantSubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ProcessPaymentWebhookHandler>> _logger = new();
    private readonly Mock<IPaymentWebhookSecretProvider> _secretProvider = new();

    private ProcessPaymentWebhookHandler CreateSut() =>
        new(_subscriptionRepo.Object, _uow.Object, _logger.Object, _secretProvider.Object);

    [Fact]
    public async Task Handle_InvalidSignature_ShouldReturnFail()
    {
        _secretProvider.Setup(s => s.GetSecret("stripe")).Returns("whsec_test");

        var sut = CreateSut();
        var command = new ProcessPaymentWebhookCommand("stripe", "{}", "invalid_sig");

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("signature");
    }

    [Fact]
    public async Task Handle_NullSignature_ShouldReturnFail()
    {
        _secretProvider.Setup(s => s.GetSecret("stripe")).Returns("whsec_test");

        var sut = CreateSut();
        var command = new ProcessPaymentWebhookCommand("stripe", "{}", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnknownProvider_ShouldHandleGracefully()
    {
        _secretProvider.Setup(s => s.GetSecret("unknown")).Returns((string?)null);

        var sut = CreateSut();
        var command = new ProcessPaymentWebhookCommand("unknown", "{\"type\":\"test\"}", null);

        var result = await sut.Handle(command, CancellationToken.None);

        // Should fail or handle gracefully — no crash
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MalformedJson_ShouldReturnFail()
    {
        _secretProvider.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);

        var sut = CreateSut();
        var command = new ProcessPaymentWebhookCommand("stripe", "not-json{{{", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}
