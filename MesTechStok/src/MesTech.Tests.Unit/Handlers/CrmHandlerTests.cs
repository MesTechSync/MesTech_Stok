using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for CRM handlers — Campaign, Deal, Lead, Loyalty.
/// </summary>
[Trait("Category", "Unit")]
public class CrmHandlerTests
{
    private readonly Mock<ICampaignRepository> _campaignRepo = new();
    private readonly Mock<ICrmDealRepository> _dealRepo = new();
    private readonly Mock<ICrmLeadRepository> _leadRepo = new();
    private readonly Mock<ILoyaltyTransactionRepository> _loyaltyTxRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ CreateCampaignHandler ═══════

    [Fact]
    public async Task CreateCampaign_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new CreateCampaignHandler(_campaignRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ DeactivateCampaignHandler ═══════

    [Fact]
    public async Task DeactivateCampaign_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new DeactivateCampaignHandler(_campaignRepo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetActiveCampaignsHandler ═══════

    [Fact]
    public async Task GetActiveCampaigns_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetActiveCampaignsHandler(_campaignRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveCampaigns_EmptyRepo_ReturnsEmptyResult()
    {
        _campaignRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign>());

        var sut = new GetActiveCampaignsHandler(_campaignRepo.Object);
        var result = await sut.Handle(
            new GetActiveCampaignsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    // ═══════ GetDealsHandler ═══════

    [Fact]
    public async Task GetDeals_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetDealsHandler(_dealRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetDeals_EmptyRepo_ReturnsEmptyResult()
    {
        _dealRepo.Setup(r => r.GetByTenantPagedAsync(
                _tenantId, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>());

        var sut = new GetDealsHandler(_dealRepo.Object);
        var result = await sut.Handle(
            new GetDealsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ═══════ GetLeadsHandler ═══════

    [Fact]
    public async Task GetLeads_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetLeadsHandler(_leadRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetLeads_EmptyRepo_ReturnsEmptyResult()
    {
        _leadRepo.Setup(r => r.GetPagedAsync(
                _tenantId, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lead>() as IReadOnlyList<Lead>, 0));

        var sut = new GetLeadsHandler(_leadRepo.Object);
        var result = await sut.Handle(
            new GetLeadsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ═══════ GetCustomerPointsHandler ═══════

    [Fact]
    public async Task GetCustomerPoints_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetCustomerPointsHandler(_loyaltyTxRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCustomerPoints_EmptyRepo_ReturnsZeroBalance()
    {
        var customerId = Guid.NewGuid();
        _loyaltyTxRepo.Setup(r => r.GetPointsSumByTypeAsync(
                _tenantId, customerId, It.IsAny<MesTech.Domain.Enums.LoyaltyTransactionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _loyaltyTxRepo.Setup(r => r.GetByCustomerPagedAsync(
                _tenantId, customerId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoyaltyTransaction>());

        var sut = new GetCustomerPointsHandler(_loyaltyTxRepo.Object);
        var result = await sut.Handle(
            new MesTech.Application.Features.Crm.Queries.GetCustomerPoints.GetCustomerPointsQuery(_tenantId, customerId), CancellationToken.None);

        result.AvailableBalance.Should().Be(0);
        result.TransactionHistory.Should().BeEmpty();
    }
}
