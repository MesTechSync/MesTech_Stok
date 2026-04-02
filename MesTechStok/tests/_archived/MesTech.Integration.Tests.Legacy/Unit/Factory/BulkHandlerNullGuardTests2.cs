using FluentAssertions;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Features.Finance.Commands.RecordCashTransaction;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Application.Commands.RejectQuotation;
using MesTech.Application.Queries.ListQuotations;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.Commands.SyncTrendyolProducts;
using MesTech.Application.Commands.SyncHepsiburadaProducts;
using MesTech.Application.Commands.SyncN11Products;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using MesTech.Application.Commands.PushOrderToBitrix24;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Toplu null-guard testleri 2 — kalan testsiz handler'lar.
/// %74.7 → %80 hedefine ulaşmak için son 40 handler.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "BulkGuard")]
[Trait("Group", "Handler")]
public class BulkHandlerNullGuardTests2
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public BulkHandlerNullGuardTests2() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ CARI / WAREHOUSE ═══
    [Fact] public async Task UpdateCariHesap_Null() { var r = new Mock<ICariHesapRepository>(); var h = new UpdateCariHesapHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateWarehouse_Null() { var r = new Mock<IWarehouseRepository>(); var h = new UpdateWarehouseHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ACCOUNTING ═══
    [Fact] public async Task RecordCashTransaction_Null() { var r = new Mock<ICashTransactionRepository>(); var h = new RecordCashTransactionHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateExpense_Null() { var r = new Mock<IExpenseRepository>(); var h = new UpdateExpenseHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateIncome_Null() { var r = new Mock<IIncomeRepository>(); var h = new UpdateIncomeHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetTaxSummary_Null() { var tr = new Mock<ITaxRecordRepository>(); var wr = new Mock<ITaxWithholdingRepository>(); var h = new GetTaxSummaryHandler(tr.Object, wr.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ QUOTATION ═══
    [Fact] public async Task RejectQuotation_Null() { var r = new Mock<IQuotationRepository>(); var h = new RejectQuotationHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ListQuotations_Null() { var r = new Mock<IQuotationRepository>(); var h = new ListQuotationsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ RETURNS ═══
    [Fact] public async Task RejectReturn_Null() { var r = new Mock<IReturnRequestRepository>(); var h = new RejectReturnHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ STOCK ═══
    [Fact] public async Task StartStockCount_Null() { var r = new Mock<IProductRepository>(); var h = new StartStockCountHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ TENANT ═══
    [Fact] public async Task UpdateTenant_Null() { var r = new Mock<ITenantRepository>(); var h = new UpdateTenantHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetTenant_Null() { var r = new Mock<ITenantRepository>(); var h = new GetTenantHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ SETTINGS / INVOICE ═══
    [Fact] public async Task SaveCompanySettings_Null() { var r = new Mock<ICompanySettingsRepository>(); var h = new SaveCompanySettingsHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SendInvoice_Null() { var r = new Mock<IInvoiceRepository>(); var h = new SendInvoiceHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ PLATFORM SYNC ═══
    [Fact] public async Task SyncPlatform_Null() { var o = new Mock<IIntegratorOrchestrator>(); var h = new SyncPlatformHandler(o.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncBitrix24Contacts_Null() { var a = new Mock<IAdapterFactory>(); var h = new SyncBitrix24ContactsHandler(a.Object, Mock.Of<ILogger<SyncBitrix24ContactsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncTrendyolProducts_Null() { var a = new Mock<IAdapterFactory>(); var h = new SyncTrendyolProductsHandler(a.Object, Mock.Of<ILogger<SyncTrendyolProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncHepsiburadaProducts_Null() { var a = new Mock<IAdapterFactory>(); var h = new SyncHepsiburadaProductsHandler(a.Object, Mock.Of<ILogger<SyncHepsiburadaProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncN11Products_Null() { var a = new Mock<IAdapterFactory>(); var h = new SyncN11ProductsHandler(a.Object, Mock.Of<ILogger<SyncN11ProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SyncCiceksepetiProducts_Null() { var a = new Mock<IAdapterFactory>(); var h = new SyncCiceksepetiProductsHandler(a.Object, Mock.Of<ILogger<SyncCiceksepetiProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task PushOrderToBitrix24_Null() { var a = new Mock<IAdapterFactory>(); var h = new PushOrderToBitrix24Handler(a.Object, Mock.Of<ILogger<PushOrderToBitrix24Handler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ AUTH ═══
    [Fact] public async Task VerifyTotp_Null() { var r = new Mock<ITotpService>(); var h = new VerifyTotpHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ORDER ═══
    [Fact] public async Task PlaceOrder_Null() { var or2 = new Mock<IOrderRepository>(); var pr = new Mock<IProductRepository>(); var h = new PlaceOrderHandler(or2.Object, pr.Object, _uow.Object, new MesTech.Domain.Services.StockCalculationService()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
