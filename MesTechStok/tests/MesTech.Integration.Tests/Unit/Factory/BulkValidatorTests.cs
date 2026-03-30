using FluentAssertions;
using MesTech.Application.Features.Quotation.Commands.AcceptQuotation;
using MesTech.Application.Features.Quotation.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Features.Stock.Commands.AddStockLot;
using MesTech.Application.Features.Returns.Commands.ApproveReturn;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Feed.Commands.CreateFeedSource;
using MesTech.Application.Features.Feed.Commands.DeleteFeedSource;
using MesTech.Application.Features.Hr.Commands.CreateProject;
using MesTech.Application.Features.Hr.Commands.CreateWorkTask;
using MesTech.Application.Features.Hr.Commands.CompleteTask;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Application.Features.System.Logs.Commands.CleanOldLogs;
using MesTech.Application.Features.System.Logs.Commands.CreateLogEntry;
using MesTech.Application.Features.SavedReports.Commands.CreateSavedReport;
using MesTech.Application.Features.SavedReports.Commands.DeleteSavedReport;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Application.Commands.CreateCustomer;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Application.Features.Product.Commands.BulkUpdatePrice;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Features.Stock.Commands.BulkUpdateStock;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Application.Features.Invoice.Commands.BulkCreateInvoice;
using MesTech.Application.Features.EInvoice.Commands.CancelEInvoice;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Factory;

/// <summary>
/// Toplu validator testleri — 50+ untested validator kapatma.
/// Her validator: TenantId/Id NotEmpty kontrolü.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
[Trait("Group", "BulkValidator")]
public class BulkValidatorTests
{
    // ═══ QUOTATION ═══
    [Fact] public void AcceptQuotation_EmptyId_Fails() { var v = new AcceptQuotationValidator(); v.Validate(new AcceptQuotationCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void ConvertQuotationToInvoice_EmptyId_Fails() { var v = new ConvertQuotationToInvoiceValidator(); v.Validate(new ConvertQuotationToInvoiceCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ STOCK ═══
    [Fact] public void AddStockLot_EmptyTenant_Fails() { var v = new AddStockLotValidator(); v.Validate(new AddStockLotCommand(Guid.Empty, Guid.NewGuid(), "LOT-1", 10, 100m, DateTime.UtcNow)).IsValid.Should().BeFalse(); }

    // ═══ RETURN ═══
    [Fact] public void ApproveReturn_EmptyId_Fails() { var v = new ApproveReturnValidator(); v.Validate(new ApproveReturnCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }

    // ═══ ACCOUNTING ═══
    [Fact] public void CreateBaBsRecord_EmptyTenant_Fails() { var v = new CreateBaBsRecordValidator(); v.Validate(new CreateBaBsRecordCommand(Guid.Empty, "2026-03", "BA", "ABC", "1234567890", 10000m)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateAccountingExpense_EmptyTenant_Fails() { var v = new CreateAccountingExpenseValidator(); v.Validate(new CreateAccountingExpenseCommand(Guid.Empty, "Test", 100m, DateTime.UtcNow)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateFixedAsset_EmptyTenant_Fails() { var v = new CreateFixedAssetValidator(); v.Validate(new CreateFixedAssetCommand(Guid.Empty, "Bilgisayar", 25000m, DateTime.UtcNow, 5)).IsValid.Should().BeFalse(); }
    [Fact] public void DeactivateFixedAsset_EmptyId_Fails() { var v = new DeactivateFixedAssetValidator(); v.Validate(new DeactivateFixedAssetCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteFixedExpense_EmptyId_Fails() { var v = new DeleteFixedExpenseValidator(); v.Validate(new DeleteFixedExpenseCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void DeletePenaltyRecord_EmptyId_Fails() { var v = new DeletePenaltyRecordValidator(); v.Validate(new DeletePenaltyRecordCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteSalaryRecord_EmptyId_Fails() { var v = new DeleteSalaryRecordValidator(); v.Validate(new DeleteSalaryRecordCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteTaxRecord_EmptyId_Fails() { var v = new DeleteTaxRecordValidator(); v.Validate(new DeleteTaxRecordCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ BILLING ═══
    [Fact] public void CreateBillingInvoice_EmptyTenant_Fails() { var v = new CreateBillingInvoiceValidator(); v.Validate(new CreateBillingInvoiceCommand(Guid.Empty, Guid.NewGuid(), 100m)).IsValid.Should().BeFalse(); }
    [Fact] public void BulkCreateInvoice_EmptyOrders_Fails() { var v = new BulkCreateInvoiceValidator(); v.Validate(new BulkCreateInvoiceCommand(new List<Guid>(), InvoiceProvider.Sovos)).IsValid.Should().BeFalse(); }

    // ═══ CALENDAR ═══
    [Fact] public void CreateCalendarEvent_EmptyTenant_Fails() { var v = new CreateCalendarEventValidator(); v.Validate(new CreateCalendarEventCommand(Guid.Empty, "Toplantı", DateTime.UtcNow, DateTime.UtcNow.AddHours(1))).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteCalendarEvent_EmptyId_Fails() { var v = new DeleteCalendarEventValidator(); v.Validate(new DeleteCalendarEventCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }

    // ═══ CRM ═══
    [Fact] public void CreateDeal_EmptyTenant_Fails() { var v = new CreateDealValidator(); v.Validate(new CreateDealCommand(Guid.Empty, "Deal", Guid.NewGuid(), Guid.NewGuid(), 1000m)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateLead_EmptyTenant_Fails() { var v = new CreateLeadValidator(); v.Validate(new CreateLeadCommand(Guid.Empty, "İsim", LeadSource.Website)).IsValid.Should().BeFalse(); }
    [Fact] public void EarnPoints_EmptyTenant_Fails() { var v = new EarnPointsValidator(); v.Validate(new EarnPointsCommand(Guid.Empty, Guid.NewGuid(), 100)).IsValid.Should().BeFalse(); }
    [Fact] public void RedeemPoints_EmptyTenant_Fails() { var v = new RedeemPointsValidator(); v.Validate(new RedeemPointsCommand(Guid.Empty, Guid.NewGuid(), 50)).IsValid.Should().BeFalse(); }

    // ═══ DROPSHIPPING ═══
    [Fact] public void CreateDropshipSupplier_EmptyTenant_Fails() { var v = new CreateDropshipSupplierValidator(); v.Validate(new CreateDropshipSupplierCommand(Guid.Empty, "Tedarikçi", "https://feed.xml", "XML")).IsValid.Should().BeFalse(); }

    // ═══ FEED ═══
    [Fact] public void CreateFeedSource_EmptyTenant_Fails() { var v = new CreateFeedSourceValidator(); v.Validate(new CreateFeedSourceCommand(Guid.Empty, "Feed", "https://x.xml", "XML")).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteFeedSource_EmptyId_Fails() { var v = new DeleteFeedSourceValidator(); v.Validate(new DeleteFeedSourceCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ HR ═══
    [Fact] public void CreateProject_EmptyTenant_Fails() { var v = new CreateProjectValidator(); v.Validate(new CreateProjectCommand(Guid.Empty, "Proje")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateWorkTask_EmptyTenant_Fails() { var v = new CreateWorkTaskValidator(); v.Validate(new CreateWorkTaskCommand(Guid.Empty, Guid.NewGuid(), "Görev")).IsValid.Should().BeFalse(); }
    [Fact] public void CompleteTask_EmptyId_Fails() { var v = new CompleteTaskValidator(); v.Validate(new CompleteTaskCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void ApproveLeave_EmptyId_Fails() { var v = new ApproveLeaveValidator(); v.Validate(new ApproveLeaveCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ ONBOARDING ═══
    [Fact] public void CompleteOnboardingStep_EmptyTenant_Fails() { var v = new CompleteOnboardingStepValidator(); v.Validate(new CompleteOnboardingStepCommand(Guid.Empty, "step1")).IsValid.Should().BeFalse(); }

    // ═══ TENANT ═══
    [Fact] public void CreateTenant_EmptyName_Fails() { var v = new CreateTenantValidator(); v.Validate(new CreateTenantCommand("")).IsValid.Should().BeFalse(); }

    // ═══ FINANCE ═══
    [Fact] public void CloseCashRegister_EmptyId_Fails() { var v = new CloseCashRegisterValidator(); v.Validate(new CloseCashRegisterCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateCashRegister_EmptyTenant_Fails() { var v = new CreateCashRegisterValidator(); v.Validate(new CreateCashRegisterCommand(Guid.Empty, "Kasa 1")).IsValid.Should().BeFalse(); }

    // ═══ SYSTEM ═══
    [Fact] public void CleanOldLogs_EmptyTenant_Fails() { var v = new CleanOldLogsValidator(); v.Validate(new CleanOldLogsCommand(Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateLogEntry_EmptyTenant_Fails() { var v = new CreateLogEntryValidator(); v.Validate(new CreateLogEntryCommand(Guid.Empty, "Info", "Test")).IsValid.Should().BeFalse(); }

    // ═══ SAVED REPORTS ═══
    [Fact] public void CreateSavedReport_EmptyTenant_Fails() { var v = new CreateSavedReportValidator(); v.Validate(new CreateSavedReportCommand(Guid.Empty, "Rapor", "{}")).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteSavedReport_EmptyId_Fails() { var v = new DeleteSavedReportValidator(); v.Validate(new DeleteSavedReportCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ SUPPLIER / CUSTOMER ═══
    [Fact] public void CreateSupplier_EmptyName_Fails() { var v = new CreateSupplierValidator(); v.Validate(new CreateSupplierCommand("", "SUP-001")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateCustomer_EmptyName_Fails() { var v = new CreateCustomerValidator(); v.Validate(new CreateCustomerCommand("", "C001")).IsValid.Should().BeFalse(); }
    [Fact] public void DeleteCategory_EmptyId_Fails() { var v = new DeleteCategoryValidator(); v.Validate(new DeleteCategoryCommand(Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ PRODUCT BULK ═══
    [Fact] public void BulkUpdatePrice_EmptyTenant_Fails() { var v = new BulkUpdatePriceValidator(); v.Validate(new BulkUpdatePriceCommand(Guid.Empty, new List<MesTech.Application.Features.Product.Commands.BulkUpdatePrice.PriceUpdateItem>())).IsValid.Should().BeFalse(); }
    [Fact] public void BulkUpdateProducts_EmptyTenant_Fails() { var v = new BulkUpdateProductsValidator(); v.Validate(new BulkUpdateProductsCommand(Guid.Empty, new List<Guid>(), null, null)).IsValid.Should().BeFalse(); }
    [Fact] public void BulkUpdateStock_EmptyTenant_Fails() { var v = new BulkUpdateStockValidator(); v.Validate(new BulkUpdateStockCommand(Guid.Empty, new List<MesTech.Application.Features.Stock.Commands.BulkUpdateStock.StockUpdateItem>())).IsValid.Should().BeFalse(); }
    [Fact] public void ExportProducts_EmptyTenant_Fails() { var v = new ExportProductsValidator(); v.Validate(new ExportProductsCommand(Guid.Empty, "CSV")).IsValid.Should().BeFalse(); }

    // ═══ EINVOICE ═══
    [Fact] public void CancelEInvoice_EmptyId_Fails() { var v = new CancelEInvoiceValidator(); v.Validate(new CancelEInvoiceCommand(Guid.Empty, "İptal")).IsValid.Should().BeFalse(); }
}
