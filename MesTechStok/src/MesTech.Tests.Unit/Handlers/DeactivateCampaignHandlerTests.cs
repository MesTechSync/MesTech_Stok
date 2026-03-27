using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeactivateCampaignHandlerTests
{
    private readonly Mock<ICampaignRepository> _campaignRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeactivateCampaignHandler _sut;

    public DeactivateCampaignHandlerTests()
    {
        _sut = new DeactivateCampaignHandler(_campaignRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentCampaign_ThrowsInvalidOperationException()
    {
        var campaignId = Guid.NewGuid();
        _campaignRepoMock.Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>())).ReturnsAsync((Campaign?)null);

        var cmd = new DeactivateCampaignCommand(campaignId);
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ExistingCampaign_CallsSaveChanges()
    {
        var campaignId = Guid.NewGuid();
        var campaign = Campaign.Create(Guid.NewGuid(), "Test Campaign",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10m);
        _campaignRepoMock.Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var cmd = new DeactivateCampaignCommand(campaignId);
        await _sut.Handle(cmd, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
