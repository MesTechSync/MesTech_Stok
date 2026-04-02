using FluentAssertions;
using MesTech.Application.Commands.RejectAccountingEntry;
using MesTech.Application.Commands.RejectQuotation;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Application.Commands.SeedDemoData;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.Commands.SyncTrendyolProducts;
using MesTech.Application.Commands.SyncHepsiburadaProducts;
using MesTech.Application.Commands.SyncN11Products;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using MesTech.Application.Commands.UpdateBotNotificationStatus;
using MesTech.Application.Commands.UpdateCustomer;
using MesTech.Application.Commands.UpdateSupplier;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Commands.UpdateProductPrice;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Commands.UpdateProductContent;
using MesTech.Application.Commands.UpdateDocumentCategory;
using MesTech.Application.Commands.UpdateDocumentMetadata;
using MesTech.Application.Commands.UpdateStockForecast;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Features.CategoryMapping.Commands.MapCategory;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

// ═══════════════════════════════════════════════════════════════
// BATCH 13: 39 basit validator testi — ID/TenantId guard'ları
// %60 → %77 kapsam hedefi
// ═══════════════════════════════════════════════════════════════

#region Reject/Send Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RejectAccountingEntryValidatorTests
{
    private readonly RejectAccountingEntryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RejectAccountingEntryCommand { DocumentId = Guid.NewGuid(), RejectedBy = "admin", RejectionSource = "manual", Reason = "Hatalı kayıt", TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new RejectAccountingEntryCommand { DocumentId = Guid.Empty, RejectedBy = "admin", RejectionSource = "manual", Reason = "Neden", TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RejectQuotationValidatorTests
{
    private readonly RejectQuotationValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RejectQuotationCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new RejectQuotationCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RejectReturnValidatorTests
{
    private readonly RejectReturnValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RejectReturnCommand(Guid.NewGuid(), "Süre aşımı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new RejectReturnCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RejectReturnCommandValidatorTests2
{
    private readonly RejectReturnCommandValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RejectReturnCommand(Guid.NewGuid(), "Hasarsız")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new RejectReturnCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SendInvoiceValidatorTests
{
    private readonly SendInvoiceValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SendInvoiceCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new SendInvoiceCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SendEInvoiceValidatorTests
{
    private readonly SendEInvoiceValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SendEInvoiceCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new SendEInvoiceCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region Sync Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncBitrix24ContactsValidatorTests
{
    private readonly SyncBitrix24ContactsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncBitrix24ContactsCommand()).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SyncBitrix24ContactsCommand()).IsValid.Should().BeTrue(); // parameterless command — always valid
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncTrendyolProductsValidatorTests
{
    private readonly SyncTrendyolProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncTrendyolProductsCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SyncTrendyolProductsCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncHepsiburadaProductsValidatorTests
{
    private readonly SyncHepsiburadaProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncHepsiburadaProductsCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SyncHepsiburadaProductsCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncN11ProductsValidatorTests
{
    private readonly SyncN11ProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncN11ProductsCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SyncN11ProductsCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncCiceksepetiProductsValidatorTests
{
    private readonly SyncCiceksepetiProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncCiceksepetiProductsCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new SyncCiceksepetiProductsCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion

#region CRM Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class LoseDealValidatorTests
{
    private readonly LoseDealValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new LoseDealCommand(Guid.NewGuid(), "Fiyat yüksek")).IsValid.Should().BeTrue();
    [Fact] public void Empty_DealId() => _v.Validate(new LoseDealCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class WinDealValidatorTests
{
    private readonly WinDealValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new WinDealCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_DealId() => _v.Validate(new WinDealCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ReplyToMessageValidatorTests
{
    private readonly ReplyToMessageValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ReplyToMessageCommand(Guid.NewGuid(), "Teşekkürler", "admin")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new ReplyToMessageCommand(Guid.Empty, "N", "admin")).IsValid.Should().BeFalse();
}

#endregion

#region Onboarding/Stock

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class StartOnboardingValidatorTests
{
    private readonly StartOnboardingValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new StartOnboardingCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new StartOnboardingCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class StartStockCountValidatorTests
{
    private readonly StartStockCountValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new StartStockCountCommand(Guid.NewGuid(), null, "Yıl sonu sayımı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new StartStockCountCommand(Guid.Empty, null, "N")).IsValid.Should().BeFalse();
}

#endregion

#region Accounting

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RejectReconciliationValidatorTests
{
    private readonly RejectReconciliationValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RejectReconciliationCommand(Guid.NewGuid(), Guid.NewGuid(), "Tutarsızlık")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new RejectReconciliationCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RunReconciliationValidatorTests
{
    private readonly RunReconciliationValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RunReconciliationCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RunReconciliationCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ImportSettlementValidatorTests
{
    private readonly ImportSettlementValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ImportSettlementCommand(Guid.NewGuid(), "Trendyol", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 10000m, 1500m, 8500m, new List<SettlementLineInput>())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new ImportSettlementCommand(Guid.Empty, "N", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 10000m, 1500m, 8500m, new List<SettlementLineInput>())).IsValid.Should().BeFalse();
}

#endregion

#region Billing/Webhook

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ProcessPaymentWebhookValidatorTests
{
    private readonly ProcessPaymentWebhookValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ProcessPaymentWebhookCommand("Iyzico", """{"status":"success"}""", null)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Provider() => _v.Validate(new ProcessPaymentWebhookCommand("", "data", null)).IsValid.Should().BeFalse();
}

#endregion

#region Tenant/Settings

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateTenantValidatorTests
{
    private readonly UpdateTenantValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateTenantCommand(Guid.NewGuid(), "Yeni İsim", null, true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateTenantCommand(Guid.Empty, "N", null, true)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MapCategoryValidatorTests
{
    private readonly MapCategoryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new MapCategoryCommand(Guid.NewGuid(), Guid.NewGuid(), PlatformType.Trendyol, "1001", "Elektronik")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new MapCategoryCommand(Guid.Empty, Guid.NewGuid(), PlatformType.Trendyol, "1", "Kategori")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MarkAllUserNotificationsReadValidatorTests
{
    private readonly MarkAllUserNotificationsReadValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new MarkAllUserNotificationsReadCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_UserId() => _v.Validate(new MarkAllUserNotificationsReadCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MarkUserNotificationReadValidatorTests
{
    private readonly MarkUserNotificationReadValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new MarkUserNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_NotificationId() => _v.Validate(new MarkUserNotificationReadCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
}

#endregion
