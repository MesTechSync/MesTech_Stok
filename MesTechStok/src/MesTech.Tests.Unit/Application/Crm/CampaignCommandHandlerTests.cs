using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmCampaignCommands")]
public class CampaignCommandHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── CreateCampaignHandler ──

    [Fact]
    public async Task CreateCampaignHandler_ValidRequest_ShouldReturnNewId()
    {
        // Arrange
        var mockRepo = new Mock<ICampaignRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new CreateCampaignHandler(mockRepo.Object, mockUow.Object);
        var command = new CreateCampaignCommand(
            _tenantId, "Summer Sale",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            15m, PlatformType.Trendyol);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCampaignHandler_WithProducts_ShouldAddProducts()
    {
        // Arrange
        var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        Campaign? capturedCampaign = null;
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()))
            .Callback<Campaign, CancellationToken>((c, _) => capturedCampaign = c);
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new CreateCampaignHandler(mockRepo.Object, mockUow.Object);

        var command = new CreateCampaignCommand(
            _tenantId, "Flash Sale",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(7),
            20m, null, productIds);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCampaign.Should().NotBeNull();
        capturedCampaign!.Products.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateCampaignHandler_NullRequest_ShouldThrow()
    {
        // Arrange
        var handler = new CreateCampaignHandler(
            Mock.Of<ICampaignRepository>(), Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateCampaignHandler_NoProducts_ShouldNotAddProducts()
    {
        // Arrange
        Campaign? capturedCampaign = null;
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()))
            .Callback<Campaign, CancellationToken>((c, _) => capturedCampaign = c);
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new CreateCampaignHandler(mockRepo.Object, mockUow.Object);

        var command = new CreateCampaignCommand(
            _tenantId, "Basic Sale",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(5), 10m);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCampaign.Should().NotBeNull();
        capturedCampaign!.Products.Should().BeEmpty();
    }

    // ── DeactivateCampaignHandler ──

    [Fact]
    public async Task DeactivateCampaignHandler_ValidCampaign_ShouldDeactivate()
    {
        // Arrange
        var campaign = Campaign.Create(_tenantId, "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10m);
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new DeactivateCampaignHandler(mockRepo.Object, mockUow.Object);

        // Act
        await handler.Handle(new DeactivateCampaignCommand(campaign.Id), CancellationToken.None);

        // Assert
        campaign.IsActive.Should().BeFalse();
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateCampaignHandler_CampaignNotFound_ShouldThrow()
    {
        // Arrange
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Campaign?)null);

        var handler = new DeactivateCampaignHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(new DeactivateCampaignCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeactivateCampaignHandler_NullRequest_ShouldThrow()
    {
        // Arrange
        var handler = new DeactivateCampaignHandler(
            Mock.Of<ICampaignRepository>(), Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
