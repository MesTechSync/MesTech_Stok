using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Sprint-B B-13 — Dropshipping CQRS handler birim testleri.
/// Gerçek handler'lar Moq ile test edilir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Dropshipping")]
[Trait("CQRS", "Commands")]
public class DropshippingCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<ISupplierFeedRepository> _feedRepo = new();
    private readonly Mock<IDropshippingPoolRepository> _poolRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ITenantProvider> _tenant = new();

    public DropshippingCommandHandlerTests()
    {
        _currentUser.Setup(u => u.UserId).Returns((Guid?)UserId);
        _tenant.Setup(t => t.GetCurrentTenantId()).Returns(TenantId);
    }

    // ── CreateFeedSourceCommand ───────────────────────────────────────────────

    [Fact]
    public async Task CreateFeed_ValidCommand_ReturnsNonEmptyGuidAndCallsAddAsync()
    {
        var handler = new CreateFeedSourceCommandHandler(
            _feedRepo.Object, _currentUser.Object, _tenant.Object);
        var cmd = new CreateFeedSourceCommand(
            SupplierId: Guid.NewGuid(),
            Name: "Test Feed",
            FeedUrl: "https://supplier.com/feed.xml",
            Format: FeedFormat.Xml,
            PriceMarkupPercent: 15m,
            PriceMarkupFixed: 0m,
            SyncIntervalMinutes: 60,
            TargetPlatforms: null,
            AutoDeactivateOnZeroStock: true
        );

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        _feedRepo.Verify(r => r.AddAsync(
            It.Is<SupplierFeed>(f =>
                f.Name == "Test Feed" &&
                f.Format == FeedFormat.Xml &&
                f.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFeed_EmptyName_ValidationFails()
    {
        var validator = new CreateFeedSourceCommandValidator();
        var cmd = new CreateFeedSourceCommand(
            Guid.NewGuid(), "", "https://x.com/f.xml",
            FeedFormat.Xml, 0, 0, 60, null, false);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateFeed_InvalidUrl_ValidationFails()
    {
        var validator = new CreateFeedSourceCommandValidator();
        var cmd = new CreateFeedSourceCommand(
            Guid.NewGuid(), "Feed", "not-a-url",
            FeedFormat.Xml, 0, 0, 60, null, false);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedUrl");
    }

    [Fact]
    public async Task CreateFeed_EmptySupplierId_ValidationFails()
    {
        var validator = new CreateFeedSourceCommandValidator();
        var cmd = new CreateFeedSourceCommand(
            Guid.Empty, "Feed", "https://x.com/f.xml",
            FeedFormat.Xml, 0, 0, 60, null, false);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    [Theory]
    [InlineData(4)]    // < 5 min
    [InlineData(1441)] // > 1440 min
    public async Task CreateFeed_InvalidSyncInterval_ValidationFails(int interval)
    {
        var validator = new CreateFeedSourceCommandValidator();
        var cmd = new CreateFeedSourceCommand(
            Guid.NewGuid(), "Feed", "https://x.com/f.xml",
            FeedFormat.Xml, 0, 0, interval, null, false);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SyncIntervalMinutes");
    }

    // ── UpdateFeedSourceCommand ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateFeed_ExistingFeed_UpdatesAndCallsUpdateAsync()
    {
        var existingFeed = new SupplierFeed { Name = "Old", FeedUrl = "https://old.com/f.xml" };
        _feedRepo.Setup(r => r.GetByIdAsync(existingFeed.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFeed);

        var handler = new UpdateFeedSourceCommandHandler(_feedRepo.Object, _currentUser.Object);
        var cmd = new UpdateFeedSourceCommand(
            existingFeed.Id, "New Name", "https://new.com/f.xml",
            FeedFormat.Json, 10m, 0m, 30, null, false, true);

        var ok = await handler.Handle(cmd, CancellationToken.None);

        ok.Should().BeTrue();
        existingFeed.Name.Should().Be("New Name");
        existingFeed.Format.Should().Be(FeedFormat.Json);
        _feedRepo.Verify(r => r.UpdateAsync(existingFeed, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFeed_FeedNotFound_ThrowsKeyNotFoundException()
    {
        _feedRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierFeed?)null);

        var handler = new UpdateFeedSourceCommandHandler(_feedRepo.Object, _currentUser.Object);
        var cmd = new UpdateFeedSourceCommand(
            Guid.NewGuid(), "Name", "https://x.com/f.xml",
            FeedFormat.Xml, 0, 0, 60, null, false, true);

        Func<Task> act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── CreateDropshippingPoolCommand ─────────────────────────────────────────

    [Fact]
    public async Task CreatePool_ValidCommand_ReturnsNonEmptyGuidAndCallsAddAsync()
    {
        var handler = new CreateDropshippingPoolCommandHandler(
            _poolRepo.Object, _currentUser.Object, _tenant.Object);
        var cmd = new CreateDropshippingPoolCommand(
            "Test Havuzu", "Açıklama", false, PoolPricingStrategy.Markup);

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        _poolRepo.Verify(r => r.AddAsync(
            It.Is<DropshippingPool>(p =>
                p.Name == "Test Havuzu" &&
                p.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePool_EmptyName_ValidationFails()
    {
        var validator = new CreateDropshippingPoolCommandValidator();
        var cmd = new CreateDropshippingPoolCommand("", null, false, PoolPricingStrategy.Fixed);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // ── AddProductToPoolCommand ───────────────────────────────────────────────

    [Fact]
    public async Task AddProductToPool_ValidCommand_CreatesPoolProductAndCallsRepo()
    {
        var pool = new DropshippingPool(TenantId, "Pool");
        var product = new Product { Name = "Ürün" };
        _poolRepo.Setup(r => r.GetByIdAsync(pool.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var handler = new AddProductToPoolCommandHandler(
            _poolRepo.Object, _productRepo.Object, _tenant.Object, _currentUser.Object);
        var cmd = new AddProductToPoolCommand(pool.Id, product.Id, null, 99.90m);

        var poolProductId = await handler.Handle(cmd, CancellationToken.None);

        poolProductId.Should().NotBeEmpty();
        _poolRepo.Verify(r => r.AddPoolProductAsync(
            It.Is<DropshippingPoolProduct>(pp =>
                pp.PoolId == pool.Id &&
                pp.ProductId == product.Id &&
                pp.PoolPrice == 99.90m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddProductToPool_PoolNotFound_ThrowsKeyNotFoundException()
    {
        _poolRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshippingPool?)null);

        var handler = new AddProductToPoolCommandHandler(
            _poolRepo.Object, _productRepo.Object, _tenant.Object, _currentUser.Object);
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), null, 10m);

        Func<Task> act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AddProductToPool_ProductNotFound_ThrowsKeyNotFoundException()
    {
        var pool = new DropshippingPool(TenantId, "Pool");
        _poolRepo.Setup(r => r.GetByIdAsync(pool.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var handler = new AddProductToPoolCommandHandler(
            _poolRepo.Object, _productRepo.Object, _tenant.Object, _currentUser.Object);
        var cmd = new AddProductToPoolCommand(pool.Id, Guid.NewGuid(), null, 10m);

        Func<Task> act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AddProductToPool_NegativePrice_ValidationFails()
    {
        var validator = new AddProductToPoolCommandValidator();
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), null, -1m);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolPrice");
    }

    // ── GetDropshippingPoolsQuery ─────────────────────────────────────────────

    [Fact]
    public async Task GetPools_CallsRepoWithTenantIdAndReturnsMappedDtos()
    {
        var pool = new DropshippingPool(TenantId, "Pool");
        _poolRepo.Setup(r => r.GetPoolsPagedAsync(
                TenantId, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<DropshippingPool>)new[] { pool }, 1));

        var handler = new GetDropshippingPoolsQueryHandler(_poolRepo.Object, _tenant.Object);
        var result = await handler.Handle(new GetDropshippingPoolsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Pool");
    }

    [Fact]
    public async Task GetPools_EmptyResult_ReturnsEmptyPage()
    {
        _poolRepo.Setup(r => r.GetPoolsPagedAsync(
                TenantId, It.IsAny<bool?>(), 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<DropshippingPool>)Array.Empty<DropshippingPool>(), 0));

        var handler = new GetDropshippingPoolsQueryHandler(_poolRepo.Object, _tenant.Object);
        var result = await handler.Handle(
            new GetDropshippingPoolsQuery(IsActive: true), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
