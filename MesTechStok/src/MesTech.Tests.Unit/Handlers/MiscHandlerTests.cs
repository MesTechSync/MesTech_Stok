using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class MiscHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── ApplyCampaignDiscountHandler ─────────────────────────────

    [Fact]
    public async Task ApplyCampaignDiscount_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICampaignRepository>();
        var pricing = new PricingService();
        var sut = new ApplyCampaignDiscountHandler(repo.Object, pricing);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ApplyCampaignDiscount_NoCampaigns_ReturnsSamePrice()
    {
        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.GetActiveByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Campaign>());

        var pricing = new PricingService();
        var sut = new ApplyCampaignDiscountHandler(repo.Object, pricing);
        var query = new ApplyCampaignDiscountQuery(Guid.NewGuid(), 100m);

        var result = await sut.Handle(query, CancellationToken.None);

        result.OriginalPrice.Should().Be(100m);
        result.DiscountedPrice.Should().Be(100m);
        result.DiscountPercent.Should().Be(0);
    }

    // ── ApproveLeaveHandler ──────────────────────────────────────

    [Fact]
    public async Task ApproveLeave_LeaveNotFound_ThrowsInvalidOperation()
    {
        var leaveRepo = new Mock<ILeaveRepository>();
        var uow = new Mock<IUnitOfWork>();

        leaveRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        var sut = new ApproveLeaveHandler(leaveRepo.Object, uow.Object);
        var cmd = new ApproveLeaveCommand(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ── EarnPointsHandler ────────────────────────────────────────

    [Fact]
    public async Task EarnPoints_NullRequest_ThrowsArgumentNullException()
    {
        var programRepo = new Mock<ILoyaltyProgramRepository>();
        var transactionRepo = new Mock<ILoyaltyTransactionRepository>();
        var uow = new Mock<IUnitOfWork>();

        var sut = new EarnPointsHandler(programRepo.Object, transactionRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task EarnPoints_NoProgramFound_ThrowsInvalidOperation()
    {
        var programRepo = new Mock<ILoyaltyProgramRepository>();
        var transactionRepo = new Mock<ILoyaltyTransactionRepository>();
        var uow = new Mock<IUnitOfWork>();

        programRepo.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyProgram?)null);

        var sut = new EarnPointsHandler(programRepo.Object, transactionRepo.Object, uow.Object);
        var cmd = new EarnPointsCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid(), 150m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ── FetchProductFromUrlHandler ───────────────────────────────

    [Fact]
    public async Task FetchProductFromUrl_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new FetchProductFromUrlHandler(
            NullLogger<FetchProductFromUrlHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FetchProductFromUrl_EmptyUrl_ReturnsNull()
    {
        var sut = new FetchProductFromUrlHandler(
            NullLogger<FetchProductFromUrlHandler>.Instance);
        var cmd = new FetchProductFromUrlCommand("");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchProductFromUrl_InvalidUrl_ReturnsNull()
    {
        var sut = new FetchProductFromUrlHandler(
            NullLogger<FetchProductFromUrlHandler>.Instance);
        var cmd = new FetchProductFromUrlCommand("not-a-url");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchProductFromUrl_TrendyolUrl_IdentifiesPlatform()
    {
        var sut = new FetchProductFromUrlHandler(
            NullLogger<FetchProductFromUrlHandler>.Instance);
        var cmd = new FetchProductFromUrlCommand("https://www.trendyol.com/product/12345");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Attributes["Platform"].Should().Be("Trendyol");
    }

    [Fact]
    public async Task FetchProductFromUrl_UnknownDomain_ReturnsNull()
    {
        var sut = new FetchProductFromUrlHandler(
            NullLogger<FetchProductFromUrlHandler>.Instance);
        var cmd = new FetchProductFromUrlCommand("https://www.unknownshop.com/product/1");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetBitrix24DealStatusHandler ─────────────────────────────

    [Fact]
    public async Task GetBitrix24DealStatus_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IBitrix24DealRepository>();
        var sut = new GetBitrix24DealStatusHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBitrix24DealStatus_DealNotFound_ReturnsNull()
    {
        var repo = new Mock<IBitrix24DealRepository>();
        repo.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);

        var sut = new GetBitrix24DealStatusHandler(repo.Object);
        var query = new GetBitrix24DealStatusQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetEmployeesHandler ──────────────────────────────────────

    [Fact]
    public async Task GetEmployees_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IEmployeeRepository>();
        var sut = new GetEmployeesHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetEmployees_EmptyList_ReturnsEmpty()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(r => r.GetByTenantAsync(
                It.IsAny<Guid>(), It.IsAny<EmployeeStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Employee>());

        var sut = new GetEmployeesHandler(repo.Object);
        var query = new GetEmployeesQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── GetPipelineKanbanHandler ─────────────────────────────────

    [Fact]
    public async Task GetPipelineKanban_PipelineNotFound_ThrowsInvalidOperation()
    {
        var dealRepo = new Mock<ICrmDealRepository>();
        var pipelineRepo = new Mock<IPipelineRepository>();

        pipelineRepo.Setup(r => r.GetByIdWithStagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var sut = new GetPipelineKanbanHandler(dealRepo.Object, pipelineRepo.Object);
        var query = new GetPipelineKanbanQuery(_tenantId, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(query, CancellationToken.None));
    }

    // ── GetPlatformMessagesHandler ───────────────────────────────

    [Fact]
    public async Task GetPlatformMessages_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPlatformMessageRepository>();
        var sut = new GetPlatformMessagesHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetPlatformMessages_EmptyResult_ReturnsEmptyWithZeroCount()
    {
        var repo = new Mock<IPlatformMessageRepository>();
        repo.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<PlatformType?>(), It.IsAny<MessageStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<PlatformMessage>() as IReadOnlyList<PlatformMessage>, 0));

        var sut = new GetPlatformMessagesHandler(repo.Object);
        var query = new GetPlatformMessagesQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GetPlatformSyncStatusHandler ─────────────────────────────

    [Fact]
    public async Task GetPlatformSyncStatus_NullRequest_ThrowsArgumentNullException()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var adapterFactory = new Mock<IAdapterFactory>();
        var sut = new GetPlatformSyncStatusHandler(storeRepo.Object, adapterFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<GetPlatformSyncStatusHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetPlatformSyncStatus_NoStores_ReturnsEmpty()
    {
        var storeRepo = new Mock<IStoreRepository>();
        var adapterFactory = new Mock<IAdapterFactory>();

        storeRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Store>());

        var sut = new GetPlatformSyncStatusHandler(storeRepo.Object, adapterFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<GetPlatformSyncStatusHandler>.Instance);
        var query = new GetPlatformSyncStatusQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
