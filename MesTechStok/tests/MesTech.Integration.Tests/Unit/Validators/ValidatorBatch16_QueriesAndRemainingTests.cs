using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Commands.SeedDemoData;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Application.Commands.UpdateBotNotificationStatus;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

// ═══════════════════════════════════════════════════════════════
// BATCH 16: Query validators + remaining commands — son 29 validator
// %82 → %95 kapsam hedefi
// ═══════════════════════════════════════════════════════════════

#region System Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetAuditLogsValidatorTests
{
    private readonly GetAuditLogsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetAuditLogsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetAuditLogsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetBackupHistoryValidatorTests
{
    private readonly GetBackupHistoryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetBackupHistoryQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetBackupHistoryQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Accounting Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetCashFlowTrendValidatorTests
{
    private readonly GetCashFlowTrendValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetCashFlowTrendQuery(Guid.NewGuid(), 12)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetCashFlowTrendQuery(Guid.Empty, 12)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetIncomeExpenseListValidatorTests
{
    private readonly GetIncomeExpenseListValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetIncomeExpenseListQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetIncomeExpenseListQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetIncomeExpenseSummaryValidatorTests
{
    private readonly GetIncomeExpenseSummaryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetIncomeExpenseSummaryQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetIncomeExpenseSummaryQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Settings Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetCredentialsSettingsValidatorTests
{
    private readonly GetCredentialsSettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetCredentialsSettingsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetCredentialsSettingsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetGeneralSettingsValidatorTests
{
    private readonly GetGeneralSettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetGeneralSettingsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetGeneralSettingsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetProfileSettingsValidatorTests
{
    private readonly GetProfileSettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetProfileSettingsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_UserId() => _v.Validate(new GetProfileSettingsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region ERP Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetErpDashboardValidatorTests
{
    private readonly GetErpDashboardValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetErpDashboardQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetErpDashboardQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetErpSyncHistoryValidatorTests
{
    private readonly GetErpSyncHistoryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetErpSyncHistoryQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetErpSyncHistoryQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetErpSyncLogsValidatorTests
{
    private readonly GetErpSyncLogsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetErpSyncLogsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetErpSyncLogsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Fulfillment Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetFulfillmentDashboardValidatorTests
{
    private readonly GetFulfillmentDashboardValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetFulfillmentDashboardQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetFulfillmentDashboardQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetFulfillmentShipmentsValidatorTests
{
    private readonly GetFulfillmentShipmentsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetFulfillmentShipmentsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetFulfillmentShipmentsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Logging Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetLogCountValidatorTests
{
    private readonly GetLogCountValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetLogCountQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetLogCountQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetLogsValidatorTests
{
    private readonly GetLogsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetLogsQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GetLogsQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Tenant Queries

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetTenantsValidatorTests
{
    private readonly GetTenantsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetTenantsQuery()).IsValid.Should().BeTrue();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GetTenantValidatorTests
{
    private readonly GetTenantValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GetTenantQuery(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new GetTenantQuery(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Product Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExportProductsValidatorTests
{
    private readonly ExportProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ExportProductsCommand(Guid.NewGuid(), "xlsx")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new ExportProductsCommand(Guid.Empty, "xlsx")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExecuteBulkImportValidatorTests
{
    private readonly ExecuteBulkImportValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ExecuteBulkImportCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new ExecuteBulkImportCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ValidateBulkImportValidatorTests
{
    private readonly ValidateBulkImportValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ValidateBulkImportCommand(Guid.NewGuid(), new byte[] { 1, 2, 3 }, "products.xlsx")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new ValidateBulkImportCommand(Guid.Empty, new byte[] { 1 }, "f.xlsx")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProductValidatorTests2
{
    private readonly UpdateProductValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProductCommand(Guid.NewGuid(), "Ürün A", "SKU-001")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateProductCommand(Guid.Empty, "N", "S")).IsValid.Should().BeFalse();
}

#endregion

#region CRM — Points

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class EarnPointsCommandValidatorTests
{
    private readonly EarnPointsCommandValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new EarnPointsCommand(Guid.NewGuid(), Guid.NewGuid(), 100)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new EarnPointsCommand(Guid.Empty, Guid.NewGuid(), 100)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RedeemPointsValidatorTests
{
    private readonly RedeemPointsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RedeemPointsCommand(Guid.NewGuid(), Guid.NewGuid(), 50)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RedeemPointsCommand(Guid.Empty, Guid.NewGuid(), 50)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RedeemPointsCommandValidatorTests
{
    private readonly RedeemPointsCommandValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RedeemPointsCommand(Guid.NewGuid(), Guid.NewGuid(), 25)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RedeemPointsCommand(Guid.Empty, Guid.NewGuid(), 25)).IsValid.Should().BeFalse();
}

#endregion

#region Misc Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SeedDemoDataValidatorTests
{
    private readonly SeedDemoDataValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SeedDemoDataCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SeedDemoDataCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SaveCompanySettingsValidatorTests
{
    private readonly SaveCompanySettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SaveCompanySettingsCommand(Guid.NewGuid(), "MesTech Ltd.")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SaveCompanySettingsCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateBotNotificationStatusValidatorTests
{
    private readonly UpdateBotNotificationStatusValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateBotNotificationStatusCommand(Guid.NewGuid(), true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new UpdateBotNotificationStatusCommand(Guid.Empty, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UploadAccountingDocumentValidatorTests
{
    private readonly UploadAccountingDocumentValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UploadAccountingDocumentCommand(Guid.NewGuid(), "fatura.pdf", new byte[] { 1, 2 })).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new UploadAccountingDocumentCommand(Guid.Empty, "f", new byte[] { 1 })).IsValid.Should().BeFalse();
}

#endregion
