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

    // ═══════ GetDealsHandler ═══════

    [Fact]
    public async Task GetDeals_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetDealsHandler(_dealRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetLeadsHandler ═══════

    [Fact]
    public async Task GetLeads_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetLeadsHandler(_leadRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetCustomerPointsHandler ═══════

    [Fact]
    public async Task GetCustomerPoints_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetCustomerPointsHandler(_loyaltyTxRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
