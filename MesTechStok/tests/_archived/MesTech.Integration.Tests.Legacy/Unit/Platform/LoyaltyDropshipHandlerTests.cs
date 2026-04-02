using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Loyalty")]
[Trait("Group", "Handler")]
public class LoyaltyDropshipHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public LoyaltyDropshipHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ LOYALTY ═══
    [Fact] public async Task EarnPoints_Null_Throws() { var lp = new Mock<ILoyaltyProgramRepository>(); var lt = new Mock<ILoyaltyTransactionRepository>(); var h = new EarnPointsHandler(lp.Object, lt.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task RedeemPoints_Null_Throws() { var lp = new Mock<ILoyaltyProgramRepository>(); var lt = new Mock<ILoyaltyTransactionRepository>(); var h = new RedeemPointsHandler(lp.Object, lt.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCustomerPoints_Null_Throws() { var r = new Mock<ILoyaltyTransactionRepository>(); var h = new GetCustomerPointsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ DROPSHIPPING ═══
    [Fact] public async Task SyncDropshipProducts_Null_Throws() { var sr = new Mock<IDropshipSupplierRepository>(); var pr = new Mock<IDropshipProductRepository>(); var ff = new Mock<IDropshipFeedFetcher>(); var h = new SyncDropshipProductsHandler(sr.Object, pr.Object, _uow.Object, ff.Object, Mock.Of<ILogger<SyncDropshipProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncSupplierPrices_Null_Throws() { var sr = new Mock<IDropshipSupplierRepository>(); var dp = new Mock<IDropshipProductRepository>(); var pr = new Mock<IProductRepository>(); var ff = new Mock<IDropshipFeedFetcher>(); var h = new SyncSupplierPricesHandler(sr.Object, dp.Object, pr.Object, ff.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ImportFromFeed_Null_Throws() { var sfr = new Mock<ISupplierFeedRepository>(); var dpr = new Mock<IDropshipProductRepository>(); var h = new ImportFromFeedHandler(sfr.Object, dpr.Object, _uow.Object, Enumerable.Empty<IFeedParserService>(), Mock.Of<ILogger<ImportFromFeedHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task PreviewFeed_Null_Throws() { var sfr = new Mock<ISupplierFeedRepository>(); var dpr = new Mock<IDropshipProductRepository>(); var h = new PreviewFeedHandler(sfr.Object, dpr.Object, Enumerable.Empty<IFeedParserService>(), Mock.Of<ILogger<PreviewFeedHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDropshipSuppliers_Null_Throws() { var r = new Mock<IDropshipSupplierRepository>(); var h = new GetDropshipSuppliersHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
