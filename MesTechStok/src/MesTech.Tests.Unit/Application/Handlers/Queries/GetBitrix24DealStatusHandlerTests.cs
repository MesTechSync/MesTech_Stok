using FluentAssertions;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetBitrix24DealStatusHandlerTests
{
    private readonly Mock<IBitrix24DealRepository> _dealRepo = new();

    private GetBitrix24DealStatusHandler CreateHandler() => new(_dealRepo.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new GetBitrix24DealStatusHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dealRepository");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_DealNotFound_ShouldReturnNull()
    {
        var orderId = Guid.NewGuid();
        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);

        var handler = CreateHandler();
        var query = new GetBitrix24DealStatusQuery(orderId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DealFound_ShouldReturnMappedDto()
    {
        var orderId = Guid.NewGuid();
        var deal = new Bitrix24Deal
        {
            TenantId = Guid.NewGuid(),
            OrderId = orderId,
            ExternalDealId = "B24-12345",
            Title = "Test Deal",
            Opportunity = 5000m,
            StageId = "WON",
            Currency = "TRY",
            SyncStatus = SyncStatus.Synced,
            LastSyncDate = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            SyncError = null
        };

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var handler = CreateHandler();
        var query = new GetBitrix24DealStatusQuery(orderId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Bitrix24DealId.Should().Be(deal.Id);
        result.ExternalDealId.Should().Be("B24-12345");
        result.OrderId.Should().Be(orderId);
        result.Title.Should().Be("Test Deal");
        result.Opportunity.Should().Be(5000m);
        result.StageId.Should().Be("WON");
        result.Currency.Should().Be("TRY");
        result.SyncStatus.Should().Be("Synced");
        result.LastSyncDate.Should().Be(new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc));
        result.SyncError.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DealWithSyncError_ShouldIncludeError()
    {
        var orderId = Guid.NewGuid();
        var deal = new Bitrix24Deal
        {
            TenantId = Guid.NewGuid(),
            OrderId = orderId,
            ExternalDealId = "B24-99999",
            Title = "Failed Deal",
            SyncStatus = SyncStatus.Failed,
            SyncError = "API timeout"
        };

        _dealRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetBitrix24DealStatusQuery(orderId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.SyncStatus.Should().Be("Failed");
        result.SyncError.Should().Be("API timeout");
    }
}
