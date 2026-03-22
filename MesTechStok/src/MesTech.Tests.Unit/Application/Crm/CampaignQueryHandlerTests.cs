using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmCampaignQueries")]
public class CampaignQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── ApplyCampaignDiscountHandler ──

    [Fact]
    public async Task ApplyCampaignDiscount_NoCampaigns_ShouldReturnOriginalPrice()
    {
        // Arrange
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetActiveByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign>().AsReadOnly());
        var pricingService = new PricingService();

        var handler = new ApplyCampaignDiscountHandler(mockRepo.Object, pricingService);
        var query = new ApplyCampaignDiscountQuery(Guid.NewGuid(), 100m);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.OriginalPrice.Should().Be(100m);
        result.DiscountedPrice.Should().Be(100m);
        result.DiscountPercent.Should().Be(0);
    }

    [Fact]
    public async Task ApplyCampaignDiscount_ActiveCampaign_ShouldApplyBestDiscount()
    {
        // Arrange
        var campaign = Campaign.Create(
            _tenantId, "Big Sale",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30), 20m);

        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetActiveByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign> { campaign }.AsReadOnly());
        var pricingService = new PricingService();

        var handler = new ApplyCampaignDiscountHandler(mockRepo.Object, pricingService);
        var query = new ApplyCampaignDiscountQuery(Guid.NewGuid(), 200m);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.OriginalPrice.Should().Be(200m);
        result.DiscountedPrice.Should().Be(160m); // 200 * 0.80
        result.DiscountPercent.Should().Be(20m);
        result.AppliedCampaignName.Should().Be("Big Sale");
    }

    [Fact]
    public async Task ApplyCampaignDiscount_NullRequest_ShouldThrow()
    {
        var handler = new ApplyCampaignDiscountHandler(
            Mock.Of<ICampaignRepository>(), new PricingService());

        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetActiveCampaignsHandler ──

    [Fact]
    public async Task GetActiveCampaigns_WithActive_ShouldReturnFiltered()
    {
        // Arrange
        var activeCampaign = Campaign.Create(
            _tenantId, "Active",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30), 10m);
        var expiredCampaign = Campaign.Create(
            _tenantId, "Expired",
            DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-30), 15m);

        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign> { activeCampaign, expiredCampaign }.AsReadOnly());

        var handler = new GetActiveCampaignsHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetActiveCampaignsQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Active");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetActiveCampaigns_EmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var mockRepo = new Mock<ICampaignRepository>();
        mockRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign>().AsReadOnly());

        var handler = new GetActiveCampaignsHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetActiveCampaignsQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetActiveCampaigns_NullRequest_ShouldThrow()
    {
        var handler = new GetActiveCampaignsHandler(Mock.Of<ICampaignRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
