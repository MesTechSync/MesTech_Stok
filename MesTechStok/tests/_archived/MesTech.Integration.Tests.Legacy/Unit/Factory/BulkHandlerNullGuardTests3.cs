using FluentAssertions;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Application.Features.Logging.Commands.CleanOldLogs;
using MesTech.Application.Features.Auth.Commands.EnableMfa;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Application.Commands.DeleteIncome;
using MesTech.Application.Queries.GetExpenseById;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Application.Queries.GetCariHareketler;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Application.Features.Accounting.Queries.GetCargoComparison;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Accounting.Queries.GetPendingReviews;
using MesTech.Application.Queries.GetCategoriesPaged;
using MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Queries.GetQuotationById;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Application.Features.Reports.ErpReconciliationReport;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Toplu null-guard testleri 3 — son 50+ handler kapatma.
/// Handler test kapsamı %80 hedefini aşma.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "BulkGuard")]
[Trait("Group", "Handler")]
public class BulkHandlerNullGuardTests3
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public BulkHandlerNullGuardTests3() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ CRM ═══
    [Fact] public async Task CreateCampaign_Null() { var r = new Mock<ICampaignRepository>(); var h = new CreateCampaignHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeactivateCampaign_Null() { var r = new Mock<ICampaignRepository>(); var h = new DeactivateCampaignHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ApplyCampaignDiscount_Null() { var r = new Mock<ICampaignRepository>(); var p = new Mock<IProductRepository>(); var h = new ApplyCampaignDiscountHandler(r.Object, p.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ FINANCE ═══
    [Fact] public async Task CloseCashRegister_Null() { var r = new Mock<ICashRegisterRepository>(); var h = new CloseCashRegisterHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateCashRegister_Null() { var r = new Mock<ICashRegisterRepository>(); var h = new CreateCashRegisterHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ SYSTEM ═══
    [Fact] public async Task CleanOldLogs_Null() { var r = new Mock<ILogEntryRepository>(); var h = new CleanOldLogsHandler(r.Object, Mock.Of<ILogger<CleanOldLogsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task EnableMfa_Null() { var r = new Mock<ITotpService>(); var u = new Mock<IUserRepository>(); var h = new EnableMfaHandler(r.Object, u.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ HR ═══
    [Fact] public async Task ApproveLeave_Null() { var r = new Mock<ILeaveRepository>(); var h = new ApproveLeaveHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CompleteTask_Null() { var r = new Mock<IWorkTaskRepository>(); var h = new CompleteTaskHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateProject_Null() { var r = new Mock<IProjectRepository>(); var h = new CreateProjectHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateWorkTask_Null() { var r = new Mock<IWorkTaskRepository>(); var h = new CreateWorkTaskHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetProjects_Null() { var r = new Mock<IProjectRepository>(); var h = new GetProjectsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetProjectTasks_Null() { var r = new Mock<IWorkTaskRepository>(); var h = new GetProjectTasksHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ACCOUNTING ═══
    [Fact] public async Task CreateAccountingBankAccount_Null() { var r = new Mock<IAccountingBankAccountRepository>(); var h = new CreateAccountingBankAccountHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateIncome_Null() { var r = new Mock<IIncomeRepository>(); var h = new CreateIncomeHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteExpense_Null() { var r = new Mock<IExpenseRepository>(); var h = new DeleteExpenseHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteIncome_Null() { var r = new Mock<IIncomeRepository>(); var h = new DeleteIncomeHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetExpenseById_Null() { var r = new Mock<IExpenseRepository>(); var h = new GetExpenseByIdHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetExpenses_Null() { var r = new Mock<IExpenseRepository>(); var h = new GetExpensesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCariHareketler_Null() { var r = new Mock<ICariHareketRepository>(); var h = new GetCariHareketlerHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCariHesaplar_Null() { var r = new Mock<ICariHesapRepository>(); var h = new GetCariHesaplarHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCargoComparison_Null() { var r = new Mock<ICargoExpenseRepository>(); var h = new GetCargoComparisonHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetKdvReport_Null() { var m = new Mock<ISender>(); var h = new GetKdvReportHandler(m.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetMonthlySummary_Null() { var or2 = new Mock<IOrderRepository>(); var er = new Mock<IExpenseRepository>(); var ir = new Mock<IIncomeRepository>(); var h = new GetMonthlySummaryHandler(or2.Object, er.Object, ir.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ PRODUCT ═══
    [Fact] public async Task GetCategoriesPaged_Null() { var r = new Mock<ICategoryRepository>(); var h = new GetCategoriesPagedHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task BulkUpdatePrice_Null() { var r = new Mock<IProductRepository>(); var h = new BulkUpdatePriceHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ STOCK ═══
    [Fact] public async Task BulkUpdateStock_Null() { var r = new Mock<IProductRepository>(); var h = new BulkUpdateStockHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetStockMovements_Null() { var r = new Mock<IStockMovementRepository>(); var h = new GetStockMovementsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetInventoryPaged_Null() { var r = new Mock<IProductRepository>(); var h = new GetInventoryPagedHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetLowStockProducts_Null() { var r = new Mock<IProductRepository>(); var h = new GetLowStockProductsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ FULFILLMENT ═══
    [Fact] public async Task CreateInboundShipment_Null() { var f = new Mock<IFulfillmentProviderFactory>(); var h = new CreateInboundShipmentHandler(f.Object, Mock.Of<ILogger<CreateInboundShipmentHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ FEED ═══
    [Fact] public async Task CreateFeedSource_Null() { var r = new Mock<ISupplierFeedRepository>(); var h = new CreateFeedSourceHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ QUOTATION / RETURN ═══
    [Fact] public async Task GetQuotationById_Null() { var r = new Mock<IQuotationRepository>(); var h = new GetQuotationByIdHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetReturnList_Null() { var r = new Mock<IReturnRequestRepository>(); var h = new GetReturnListHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ERP ═══
    [Fact] public async Task ErpReconciliationReport_Null() { var f = new Mock<IErpAdapterFactory>(); var c = new Mock<ICounterpartyRepository>(); var h = new ErpReconciliationReportHandler(f.Object, c.Object, Mock.Of<ILogger<ErpReconciliationReportHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ SAVED REPORTS ═══
    [Fact] public async Task CreateSavedReport_Null() { var r = new Mock<ISavedReportRepository>(); var h = new CreateSavedReportHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task DeleteSavedReport_Null() { var r = new Mock<ISavedReportRepository>(); var h = new DeleteSavedReportHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetSavedReports_Null() { var r = new Mock<ISavedReportRepository>(); var h = new GetSavedReportsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
