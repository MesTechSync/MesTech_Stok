using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CompleteOnboardingStepHandlerTests
{
    private readonly Mock<IOnboardingProgressRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CompleteOnboardingStepHandler _sut;

    public CompleteOnboardingStepHandlerTests()
    {
        _sut = new CompleteOnboardingStepHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NoOnboarding_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var cmd = new CompleteOnboardingStepCommand(Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Onboarding*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
