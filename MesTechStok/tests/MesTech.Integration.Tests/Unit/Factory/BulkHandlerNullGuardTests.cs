using FluentAssertions;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Application.Commands.UpdateCategory;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Features.Stock.Commands.AddStockLot;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Application.Features.Quotation.Commands.CreateQuotation;
using MesTech.Application.Features.Quotation.Commands.AcceptQuotation;
using MesTech.Application.Features.Quotation.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Features.Returns.Commands.ApproveReturn;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Features.Product.Queries.GetCategories;
using MesTech.Application.Features.Product.Queries.GetCategoriesPaged;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Application.Interfaces.Accounting.IJournalEntryRepository;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Toplu null-guard testleri — kalan testsiz handler'lar.
/// Her handler: null request → Exception.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "BulkGuard")]
[Trait("Group", "Handler")]
public class BulkHandlerNullGuardTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public BulkHandlerNullGuardTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ CATEGORY ═══
    [Fact] public async Task CreateCategory_Null() { var r = new Mock<ICategoryRepository>(); var h = new CreateCategoryHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteCategory_Null() { var r = new Mock<ICategoryRepository>(); var h = new DeleteCategoryHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateCategory_Null() { var r = new Mock<ICategoryRepository>(); var h = new UpdateCategoryHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCategories_Null() { var r = new Mock<ICategoryRepository>(); var h = new GetCategoriesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ PRODUCT ═══
    [Fact] public async Task CreateProduct_Null() { var r = new Mock<IProductRepository>(); var h = new CreateProductHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ WAREHOUSE ═══
    [Fact] public async Task CreateWarehouse_Null() { var r = new Mock<IWarehouseRepository>(); var h = new CreateWarehouseHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteWarehouse_Null() { var r = new Mock<IWarehouseRepository>(); var h = new DeleteWarehouseHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ STOCK ═══
    [Fact] public async Task AddStock_Null() { var r = new Mock<IProductRepository>(); var h = new AddStockHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task RemoveStock_Null() { var r = new Mock<IProductRepository>(); var h = new RemoveStockHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task TransferStock_Null() { var r = new Mock<IProductRepository>(); var w = new Mock<IWarehouseRepository>(); var h = new TransferStockHandler(r.Object, w.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task AdjustStock_Null() { var r = new Mock<IProductRepository>(); var h = new AdjustStockHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task AddStockLot_Null() { var r = new Mock<IInventoryLotRepository>(); var h = new AddStockLotHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ CARI ═══
    [Fact] public async Task CreateCariHesap_Null() { var r = new Mock<ICariHesapRepository>(); var h = new CreateCariHesapHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateCariHareket_Null() { var r = new Mock<ICariHareketRepository>(); var h = new CreateCariHareketHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ QUOTATION ═══
    [Fact] public async Task CreateQuotation_Null() { var r = new Mock<IQuotationRepository>(); var h = new CreateQuotationHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task AcceptQuotation_Null() { var r = new Mock<IQuotationRepository>(); var h = new AcceptQuotationHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ConvertQuotationToInvoice_Null() { var qr = new Mock<IQuotationRepository>(); var ir = new Mock<IInvoiceRepository>(); var h = new ConvertQuotationToInvoiceHandler(qr.Object, ir.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ RETURN ═══
    [Fact] public async Task ApproveReturn_Null() { var r = new Mock<IReturnRequestRepository>(); var h = new ApproveReturnHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ACCOUNTING QUERIES ═══
    [Fact] public async Task GetBalanceSheet_Null() { var ar = new Mock<IChartOfAccountsRepository>(); var jr = new Mock<IJournalEntryRepository>(); var h = new GetBalanceSheetHandler(ar.Object, jr.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCashFlowReport_Null() { var r = new Mock<ICashFlowEntryRepository>(); var h = new GetCashFlowReportHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CalculateDepreciation_Null() { var r = new Mock<IFixedAssetRepository>(); var s = new DepreciationCalculationService(); var h = new CalculateDepreciationHandler(r.Object, s); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ TENANT ═══
    [Fact] public async Task CreateTenant_Null() { var r = new Mock<ITenantRepository>(); var h = new CreateTenantHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetTenants_Null() { var r = new Mock<ITenantRepository>(); var h = new GetTenantsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ HR ═══
    [Fact] public async Task GetDepartments_Null() { var r = new Mock<IDepartmentRepository>(); var h = new GetDepartmentsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetEmployees_Null() { var r = new Mock<IEmployeeRepository>(); var h = new GetEmployeesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ FINANCE ═══
    [Fact] public async Task GetCashFlow_Null() { var er = new Mock<IFinanceExpenseRepository>(); var or2 = new Mock<IOrderRepository>(); var h = new GetCashFlowHandler(er.Object, or2.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetProfitLoss_Null() { var er = new Mock<IFinanceExpenseRepository>(); var or2 = new Mock<IOrderRepository>(); var h = new GetProfitLossHandler(er.Object, or2.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
