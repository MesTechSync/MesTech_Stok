using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetActiveCampaignsHandlerTests
{
    private readonly Mock<ICampaignRepository> _repository = new();

    private GetActiveCampaignsHandler CreateHandler() => new(_repository.Object);

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithActiveCampaigns_ShouldReturnMappedResults()
    {
        var tenantId = Guid.NewGuid();
        var campaign = Campaign.Create(
            tenantId, "Summer Sale",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30),
            15m);

        _repository.Setup(r => r.GetActiveByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign> { campaign }.AsReadOnly());

        var handler = CreateHandler();
        var query = new GetActiveCampaignsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Summer Sale");
        result.Items[0].DiscountPercent.Should().Be(15m);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyItems()
    {
        var tenantId = Guid.NewGuid();
        _repository.Setup(r => r.GetActiveByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetActiveCampaignsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExpiredCampaign_ShouldNotBeIncluded()
    {
        var tenantId = Guid.NewGuid();
        var expiredCampaign = Campaign.Create(
            tenantId, "Old Sale",
            DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-30),
            10m);

        _repository.Setup(r => r.GetActiveByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign> { expiredCampaign }.AsReadOnly());

        var handler = CreateHandler();
        var query = new GetActiveCampaignsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
