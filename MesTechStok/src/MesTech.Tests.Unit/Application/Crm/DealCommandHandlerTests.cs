using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
public class DealCommandHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _pipelineId = Guid.NewGuid();
    private static readonly Guid _stageId = Guid.NewGuid();

    private static Deal MakeDeal(decimal amount = 5000m)
        => Deal.Create(_tenantId, "Test Deal", _pipelineId, _stageId, amount);

    [Fact]
    public async Task WinDealHandler_ValidDeal_ShouldMarkAsWon()
    {
        var deal = MakeDeal();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new WinDealHandler(mockRepo.Object, mockUow.Object);
        await handler.Handle(new WinDealCommand(deal.Id), CancellationToken.None);

        deal.Status.Should().Be(DealStatus.Won);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WinDealHandler_WithOrderId_ShouldLinkOrder()
    {
        var deal = MakeDeal();
        var orderId = Guid.NewGuid();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);
        var mockUow = new Mock<IUnitOfWork>();

        await new WinDealHandler(mockRepo.Object, mockUow.Object)
            .Handle(new WinDealCommand(deal.Id, orderId), CancellationToken.None);

        deal.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task WinDealHandler_DealNotFound_ShouldThrow()
    {
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Deal?)null);

        var handler = new WinDealHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());
        var act = () => handler.Handle(new WinDealCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoseDealHandler_ValidDeal_ShouldMarkAsLost()
    {
        var deal = MakeDeal();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        await new LoseDealHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new LoseDealCommand(deal.Id, "Budget yok"), CancellationToken.None);

        deal.Status.Should().Be(DealStatus.Lost);
        deal.LostReason.Should().Be("Budget yok");
    }

    [Fact]
    public async Task LoseDealHandler_EmptyReason_ShouldThrow()
    {
        var deal = MakeDeal();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(deal);

        // MarkAsLost calls ArgumentException.ThrowIfNullOrWhiteSpace(reason)
        var act = () => new LoseDealHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new LoseDealCommand(deal.Id, ""), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
