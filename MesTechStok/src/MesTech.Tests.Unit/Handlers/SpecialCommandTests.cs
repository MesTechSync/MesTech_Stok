using System.IO;
using FluentAssertions;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Commands.RejectQuotation;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Application.Interfaces.Erp;
using MesTech.Application.Queries.SearchProductsForImageMatch;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for special/complex command and query handlers: CalculateDepreciation,
/// CheckVknMukellef, ImportFromFeed, LinkDropshipProduct, PlaceDropshipOrder,
/// PushOrderToBitrix24, RedeemPoints, RejectQuotation, RejectReconciliation,
/// ReturnApproved, SearchProductsForImageMatch, SyncBitrix24Contacts,
/// SyncDropshipProducts, SyncOrderToErp, SyncSupplierPrices,
/// UploadAccountingDocument, ValidateBulkImport.
/// </summary>
[Trait("Category", "Unit")]
public class SpecialCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _id = Guid.NewGuid();

    // ═══════ CalculateDepreciationHandler ═══════

    [Fact]
    public async Task CalculateDepreciation_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var service = new DepreciationCalculationService();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedAsset?)null);

        var sut = new CalculateDepreciationHandler(repo.Object, service);
        var query = new CalculateDepreciationQuery(_id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateDepreciation_ValidAsset_ReturnsResult()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var service = new DepreciationCalculationService();
        var asset = FixedAsset.Create(
            _tenantId, "Machine", "253", 10000m, DateTime.UtcNow.AddYears(-1), 5,
            DepreciationMethod.StraightLine);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        var sut = new CalculateDepreciationHandler(repo.Object, service);
        var query = new CalculateDepreciationQuery(_id);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.AssetName.Should().Be("Machine");
        result.AcquisitionCost.Should().Be(10000m);
        result.Schedule.Should().NotBeEmpty();
    }

    // ═══════ CheckVknMukellefHandler ═══════

    [Fact]
    public async Task CheckVknMukellef_NullRequest_ThrowsArgumentNullException()
    {
        var provider = new Mock<IEInvoiceProvider>();
        var sut = new CheckVknMukellefHandler(provider.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CheckVknMukellef_ValidVkn_ReturnsResult()
    {
        var provider = new Mock<IEInvoiceProvider>();
        var expected = new VknMukellefResult("1234567890", true, false, "Test Ltd", DateTime.UtcNow);
        provider.Setup(p => p.CheckVknMukellefAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new CheckVknMukellefHandler(provider.Object);
        var query = new CheckVknMukellefQuery("1234567890");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Vkn.Should().Be("1234567890");
        result.IsEInvoiceMukellef.Should().BeTrue();
    }

    // ═══════ ImportFromFeedHandler ═══════

    [Fact]
    public async Task ImportFromFeed_NullRequest_ThrowsArgumentNullException()
    {
        var feedRepo = new Mock<ISupplierFeedRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var parsers = new List<IFeedParserService>();
        var logger = new Mock<ILogger<ImportFromFeedHandler>>();
        var sut = new ImportFromFeedHandler(feedRepo.Object, productRepo.Object, uow.Object, parsers, Mock.Of<IHttpClientFactory>(), logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ImportFromFeed_FeedNotFound_ThrowsInvalidOperationException()
    {
        var feedRepo = new Mock<ISupplierFeedRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var parsers = new List<IFeedParserService>();
        var logger = new Mock<ILogger<ImportFromFeedHandler>>();
        feedRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierFeed?)null);

        var sut = new ImportFromFeedHandler(feedRepo.Object, productRepo.Object, uow.Object, parsers, Mock.Of<IHttpClientFactory>(), logger.Object);
        var cmd = new ImportFromFeedCommand(_id, new List<string> { "SKU1" }, 1.2m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ LinkDropshipProductHandler ═══════

    [Fact]
    public async Task LinkDropshipProduct_NotFound_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipProduct?)null);

        var sut = new LinkDropshipProductHandler(repo.Object, uow.Object);
        var cmd = new LinkDropshipProductCommand(_tenantId, _id, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task LinkDropshipProduct_WrongTenant_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var product = DropshipProduct.Create(Guid.NewGuid(), Guid.NewGuid(), "EXT1", "Product", 100m, 10);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var sut = new LinkDropshipProductHandler(repo.Object, uow.Object);
        var cmd = new LinkDropshipProductCommand(_tenantId, _id, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task LinkDropshipProduct_ValidRequest_ReturnsUnitValue()
    {
        var repo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var product = DropshipProduct.Create(_tenantId, Guid.NewGuid(), "EXT1", "Product", 100m, 10);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var sut = new LinkDropshipProductHandler(repo.Object, uow.Object);
        var cmd = new LinkDropshipProductCommand(_tenantId, _id, Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        repo.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ PlaceDropshipOrderHandler ═══════

    [Fact]
    public async Task PlaceDropshipOrder_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new PlaceDropshipOrderHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PlaceDropshipOrder_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new PlaceDropshipOrderHandler(repo.Object, uow.Object);
        var cmd = new PlaceDropshipOrderCommand(
            _tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SUP-REF-001");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<DropshipOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ PushOrderToBitrix24Handler ═══════

    [Fact]
    public async Task PushOrderToBitrix24_NullRequest_ThrowsArgumentNullException()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var dealRepo = new Mock<IBitrix24DealRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new PushOrderToBitrix24Handler(orderRepo.Object, dealRepo.Object, adapter.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PushOrderToBitrix24_ExistingDeal_ReturnsSuccessWithExistingId()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var dealRepo = new Mock<IBitrix24DealRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        var existingDeal = new Bitrix24Deal { ExternalDealId = "B24-123" };
        dealRepo.Setup(r => r.GetByOrderIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDeal);

        var sut = new PushOrderToBitrix24Handler(orderRepo.Object, dealRepo.Object, adapter.Object, uow.Object);
        var cmd = new PushOrderToBitrix24Command(_id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ExternalDealId.Should().Be("B24-123");
    }

    [Fact]
    public async Task PushOrderToBitrix24_OrderNotFound_ReturnsFailure()
    {
        var orderRepo = new Mock<IOrderRepository>();
        var dealRepo = new Mock<IBitrix24DealRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        dealRepo.Setup(r => r.GetByOrderIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bitrix24Deal?)null);
        orderRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var sut = new PushOrderToBitrix24Handler(orderRepo.Object, dealRepo.Object, adapter.Object, uow.Object);
        var cmd = new PushOrderToBitrix24Command(_id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // ═══════ RedeemPointsHandler ═══════

    [Fact]
    public async Task RedeemPoints_NullRequest_ThrowsArgumentNullException()
    {
        var programRepo = new Mock<ILoyaltyProgramRepository>();
        var txRepo = new Mock<ILoyaltyTransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RedeemPointsHandler(programRepo.Object, txRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RedeemPoints_NoActiveProgram_ThrowsInvalidOperationException()
    {
        var programRepo = new Mock<ILoyaltyProgramRepository>();
        var txRepo = new Mock<ILoyaltyTransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        programRepo.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyProgram?)null);

        var sut = new RedeemPointsHandler(programRepo.Object, txRepo.Object, uow.Object);
        var cmd = new RedeemPointsCommand(_tenantId, Guid.NewGuid(), 100);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task RedeemPoints_BelowMinimum_ThrowsInvalidOperationException()
    {
        var programRepo = new Mock<ILoyaltyProgramRepository>();
        var txRepo = new Mock<ILoyaltyTransactionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var program = LoyaltyProgram.Create(_tenantId, "VIP", 10, 500); // min 500
        programRepo.Setup(r => r.GetActiveByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(program);

        var sut = new RedeemPointsHandler(programRepo.Object, txRepo.Object, uow.Object);
        var cmd = new RedeemPointsCommand(_tenantId, Guid.NewGuid(), 100); // below 500

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ RejectQuotationHandler ═══════

    [Fact]
    public async Task RejectQuotation_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IQuotationRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RejectQuotationHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RejectQuotation_NotFound_ReturnsFailure()
    {
        var repo = new Mock<IQuotationRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Quotation?)null);

        var sut = new RejectQuotationHandler(repo.Object, uow.Object);
        var cmd = new RejectQuotationCommand(_id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // ═══════ RejectReconciliationHandler ═══════

    [Fact]
    public async Task RejectReconciliation_NotFound_ThrowsInvalidOperationException()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        matchRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var sut = new RejectReconciliationHandler(matchRepo.Object, uow.Object);
        var cmd = new RejectReconciliationCommand(_id, Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task RejectReconciliation_ValidMatch_RejectsAndSaves()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var match = ReconciliationMatch.Create(
            _tenantId, DateTime.UtcNow, 0.95m, ReconciliationStatus.NeedsReview);
        matchRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(match);

        var sut = new RejectReconciliationHandler(matchRepo.Object, uow.Object);
        var cmd = new RejectReconciliationCommand(_id, Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        matchRepo.Verify(r => r.UpdateAsync(match, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ ReturnApprovedStockRestorationHandler ═══════

    [Fact]
    public async Task ReturnApproved_EmptyLines_CompletesWithoutError()
    {
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<MesTech.Application.EventHandlers.ReturnApprovedStockRestorationHandler>>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var sut = new MesTech.Application.EventHandlers.ReturnApprovedStockRestorationHandler(
            productRepo.Object, uow.Object, logger.Object);

        await sut.HandleAsync(
            Guid.NewGuid(), _tenantId,
            Array.Empty<MesTech.Domain.Events.ReturnLineInfoEvent>(),
            CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnApproved_ProductNotFound_ContinuesWithoutException()
    {
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<MesTech.Application.EventHandlers.ReturnApprovedStockRestorationHandler>>();
        productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var sut = new MesTech.Application.EventHandlers.ReturnApprovedStockRestorationHandler(
            productRepo.Object, uow.Object, logger.Object);

        var lines = new List<MesTech.Domain.Events.ReturnLineInfoEvent>
        {
            new(_id, "SKU1", 2, 10m)
        };

        await sut.HandleAsync(
            Guid.NewGuid(), _tenantId, lines, CancellationToken.None);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ SearchProductsForImageMatchHandler ═══════

    [Fact]
    public async Task SearchProductsForImageMatch_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>());

        var sut = new SearchProductsForImageMatchHandler(repo.Object);
        var query = new SearchProductsForImageMatchQuery();

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ═══════ SyncBitrix24ContactsHandler ═══════

    [Fact]
    public async Task SyncBitrix24Contacts_NullRequest_ThrowsArgumentNullException()
    {
        var contactRepo = new Mock<IBitrix24ContactRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new SyncBitrix24ContactsHandler(contactRepo.Object, adapter.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SyncBitrix24Contacts_AdapterThrows_ReturnsFailure()
    {
        var contactRepo = new Mock<IBitrix24ContactRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var sut = new SyncBitrix24ContactsHandler(contactRepo.Object, adapter.Object, uow.Object);
        var cmd = new SyncBitrix24ContactsCommand();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Connection failed"));
    }

    [Fact]
    public async Task SyncBitrix24Contacts_SuccessfulSync_ReturnsSuccessWithCount()
    {
        var contactRepo = new Mock<IBitrix24ContactRepository>();
        var adapter = new Mock<IBitrix24Adapter>();
        var uow = new Mock<IUnitOfWork>();
        adapter.Setup(a => a.SyncContactsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
        contactRepo.Setup(r => r.GetUnsyncedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bitrix24Contact>());

        var sut = new SyncBitrix24ContactsHandler(contactRepo.Object, adapter.Object, uow.Object);
        var cmd = new SyncBitrix24ContactsCommand();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SyncedCount.Should().Be(5);
    }

    // ═══════ SyncDropshipProductsHandler ═══════

    [Fact]
    public async Task SyncDropshipProducts_SupplierNotFound_ThrowsInvalidOperationException()
    {
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var fetcher = new Mock<IDropshipFeedFetcher>();
        var logger = new Mock<ILogger<SyncDropshipProductsHandler>>();
        supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipSupplier?)null);

        var sut = new SyncDropshipProductsHandler(
            supplierRepo.Object, productRepo.Object, uow.Object, fetcher.Object, logger.Object);
        var cmd = new SyncDropshipProductsCommand(_tenantId, _id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task SyncDropshipProducts_WrongTenant_ThrowsInvalidOperationException()
    {
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var fetcher = new Mock<IDropshipFeedFetcher>();
        var logger = new Mock<ILogger<SyncDropshipProductsHandler>>();
        var supplier = DropshipSupplier.Create(Guid.NewGuid(), "Supplier", "http://test.com", MesTech.Domain.Dropshipping.Enums.DropshipMarkupType.Percentage, 10m); // different tenant
        supplierRepo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var sut = new SyncDropshipProductsHandler(
            supplierRepo.Object, productRepo.Object, uow.Object, fetcher.Object, logger.Object);
        var cmd = new SyncDropshipProductsCommand(_tenantId, _id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ SyncOrderToErpHandler ═══════

    [Fact]
    public async Task SyncOrderToErp_NullRequest_ThrowsArgumentNullException()
    {
        var factory = new Mock<IErpAdapterFactory>();
        var logRepo = new Mock<IErpSyncLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new SyncOrderToErpHandler(factory.Object, logRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SyncOrderToErp_AdapterThrows_ReturnsFailureAndLogsError()
    {
        var factory = new Mock<IErpAdapterFactory>();
        var logRepo = new Mock<IErpSyncLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var adapter = new Mock<IErpAdapter>();
        factory.Setup(f => f.GetAdapter(It.IsAny<ErpProvider>())).Returns(adapter.Object);
        adapter.Setup(a => a.SyncOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ERP down"));

        var sut = new SyncOrderToErpHandler(factory.Object, logRepo.Object, uow.Object);
        var cmd = new SyncOrderToErpCommand(_tenantId, Guid.NewGuid(), ErpProvider.Parasut);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ERP down");
    }

    [Fact]
    public async Task SyncOrderToErp_SuccessfulSync_ReturnsSuccessResult()
    {
        var factory = new Mock<IErpAdapterFactory>();
        var logRepo = new Mock<IErpSyncLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var adapter = new Mock<IErpAdapter>();
        factory.Setup(f => f.GetAdapter(ErpProvider.Parasut)).Returns(adapter.Object);
        var syncResult = new ErpSyncResult(true, "ERP-REF-001", null);
        adapter.Setup(a => a.SyncOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResult);

        var sut = new SyncOrderToErpHandler(factory.Object, logRepo.Object, uow.Object);
        var cmd = new SyncOrderToErpCommand(_tenantId, Guid.NewGuid(), ErpProvider.Parasut);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("ERP-REF-001");
    }

    // ═══════ SyncSupplierPricesHandler ═══════

    [Fact]
    public async Task SyncSupplierPrices_NullRequest_ThrowsArgumentNullException()
    {
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var mainRepo = new Mock<IProductRepository>();
        var fetcher = new Mock<IDropshipFeedFetcher>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new SyncSupplierPricesHandler(
            supplierRepo.Object, productRepo.Object, mainRepo.Object, fetcher.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SyncSupplierPrices_SupplierNotFound_ThrowsInvalidOperationException()
    {
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var mainRepo = new Mock<IProductRepository>();
        var fetcher = new Mock<IDropshipFeedFetcher>();
        var uow = new Mock<IUnitOfWork>();
        supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipSupplier?)null);

        var sut = new SyncSupplierPricesHandler(
            supplierRepo.Object, productRepo.Object, mainRepo.Object, fetcher.Object, uow.Object);
        var cmd = new SyncSupplierPricesCommand(_id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ UploadAccountingDocumentHandler ═══════

    [Fact]
    public async Task UploadAccountingDocument_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IAccountingDocumentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UploadAccountingDocumentHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UploadAccountingDocument_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IAccountingDocumentRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UploadAccountingDocumentHandler(repo.Object, uow.Object);
        var cmd = new UploadAccountingDocumentCommand(
            _tenantId, "invoice.pdf", "application/pdf", 1024,
            "/storage/invoice.pdf", DocumentType.PurchaseInvoice, DocumentSource.Upload);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<AccountingDocument>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ ValidateBulkImportHandler ═══════

    [Fact]
    public async Task ValidateBulkImport_NullRequest_ThrowsArgumentNullException()
    {
        var importService = new Mock<IBulkProductImportService>();
        var sut = new ValidateBulkImportHandler(importService.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateBulkImport_EmptyStream_ReturnsInvalid()
    {
        var importService = new Mock<IBulkProductImportService>();
        var sut = new ValidateBulkImportHandler(importService.Object);
        using var emptyStream = new MemoryStream();
        var cmd = new ValidateBulkImportCommand(emptyStream, "test.xlsx");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBulkImport_WrongExtension_ReturnsInvalid()
    {
        var importService = new Mock<IBulkProductImportService>();
        var sut = new ValidateBulkImportHandler(importService.Object);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ValidateBulkImportCommand(stream, "test.csv");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains(".xlsx"));
    }

    [Fact]
    public async Task ValidateBulkImport_ValidXlsx_DelegatesToService()
    {
        var importService = new Mock<IBulkProductImportService>();
        var expected = new ImportValidationResult(true, 10, 10, 0, new List<ImportRowError>());
        importService.Setup(s => s.ValidateExcelAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new ValidateBulkImportHandler(importService.Object);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cmd = new ValidateBulkImportCommand(stream, "products.xlsx");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(10);
    }
}
