using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.DeleteCampaign;
using MesTech.Application.Features.Crm.Commands.DeleteDeal;
using MesTech.Application.Features.Crm.Commands.DeleteLead;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Crm")]
public class CrmDeleteHandlerTests
{
    // ─── DeleteCampaign ───────────────────────────────────────────

    [Fact]
    public async Task DeleteCampaign_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<ICampaignRepository>();
        var uow = new Mock<IUnitOfWork>();
        var campaign = Campaign.Create(
            Guid.NewGuid(), "Test Campaign",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30),
            10m);

        repo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var handler = new DeleteCampaignHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCampaignCommand(campaign.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        campaign.IsDeleted.Should().BeTrue();
        campaign.DeletedAt.Should().NotBeNull();
        campaign.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCampaign_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<ICampaignRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        var handler = new DeleteCampaignHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCampaignCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteDeal ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteDeal_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<IDealRepository>();
        var uow = new Mock<IUnitOfWork>();
        var deal = Deal.Create(
            Guid.NewGuid(), "Test Deal",
            Guid.NewGuid(), Guid.NewGuid(), 1000m);

        repo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var handler = new DeleteDealHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteDealCommand(deal.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        deal.IsDeleted.Should().BeTrue();
        deal.DeletedAt.Should().NotBeNull();
        deal.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        repo.Verify(r => r.UpdateAsync(deal, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDeal_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<IDealRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var handler = new DeleteDealHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteDealCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        repo.Verify(r => r.UpdateAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteLead ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteLead_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<ILeadRepository>();
        var uow = new Mock<IUnitOfWork>();
        var lead = Lead.Create(
            Guid.NewGuid(), "Test Lead", LeadSource.Web,
            email: "test@example.com");

        repo.Setup(r => r.GetByIdAsync(lead.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);

        var handler = new DeleteLeadHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeadCommand(lead.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        lead.IsDeleted.Should().BeTrue();
        lead.DeletedAt.Should().NotBeNull();
        lead.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        repo.Verify(r => r.UpdateAsync(lead, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLead_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<ILeadRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var handler = new DeleteLeadHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteLeadCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        repo.Verify(r => r.UpdateAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
