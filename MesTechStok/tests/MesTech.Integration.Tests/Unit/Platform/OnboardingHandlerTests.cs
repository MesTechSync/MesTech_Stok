using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Application.Features.Onboarding.Queries.GetV5ReadinessCheck;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Onboarding")]
[Trait("Group", "Handler")]
public class OnboardingHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();

    public OnboardingHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task StartOnboarding_NullRequest_Throws()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var handler = new StartOnboardingHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteOnboardingStep_NullRequest_Throws()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var handler = new CompleteOnboardingStepHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetOnboardingProgress_NullRequest_Throws()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var handler = new GetOnboardingProgressHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetV5ReadinessCheck_NullRequest_Throws()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var erpFactory = new Mock<IErpAdapterFactory>();
        var fulfillmentFactory = new Mock<IFulfillmentProviderFactory>();
        var commissionRepo = new Mock<ICommissionRecordRepository>();
        var counterpartyRepo = new Mock<ICounterpartyRepository>();
        var logger = Mock.Of<ILogger<GetV5ReadinessCheckHandler>>();
        var handler = new GetV5ReadinessCheckHandler(
            repo.Object, erpFactory.Object, fulfillmentFactory.Object,
            commissionRepo.Object, counterpartyRepo.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
