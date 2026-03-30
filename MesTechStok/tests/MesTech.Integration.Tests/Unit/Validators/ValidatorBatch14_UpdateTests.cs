using FluentAssertions;
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
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

// ═══════════════════════════════════════════════════════════════
// BATCH 14: Update + Accounting validators — %71 → %80 hedefi
// ═══════════════════════════════════════════════════════════════

#region Update Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCustomerValidatorTests
{
    private readonly UpdateCustomerValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateCustomerCommand(Guid.NewGuid(), "Ali", "C-001")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateCustomerCommand(Guid.Empty, "Ali", "C-001")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateSupplierValidatorTests2
{
    private readonly UpdateSupplierValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateSupplierCommand(Guid.NewGuid(), "Tedarik", "S-001")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateSupplierCommand(Guid.Empty, "T", "S")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateWarehouseValidatorTests
{
    private readonly UpdateWarehouseValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateWarehouseCommand(Guid.NewGuid(), "Depo A", "WH-01")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateWarehouseCommand(Guid.Empty, "D", "W")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProductPriceValidatorTests
{
    private readonly UpdateProductPriceValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProductPriceCommand(Guid.NewGuid(), 99.90m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateProductPriceCommand(Guid.Empty, 99.90m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProductImageValidatorTests
{
    private readonly UpdateProductImageValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProductImageCommand(Guid.NewGuid(), "https://img.example.com/p1.jpg")).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateProductImageCommand(Guid.Empty, "url")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProductContentValidatorTests
{
    private readonly UpdateProductContentValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProductContentCommand(Guid.NewGuid(), "Yeni açıklama", "SEO başlık")).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateProductContentCommand(Guid.Empty, "D", "S")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDocumentCategoryValidatorTests
{
    private readonly UpdateDocumentCategoryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateDocumentCategoryCommand(Guid.NewGuid(), "Faturalar")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateDocumentCategoryCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDocumentMetadataValidatorTests
{
    private readonly UpdateDocumentMetadataValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateDocumentMetadataCommand(Guid.NewGuid(), "invoice.pdf", "Faturalar")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateDocumentMetadataCommand(Guid.Empty, "f", "c")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateStockForecastValidatorTests
{
    private readonly UpdateStockForecastValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateStockForecastCommand(Guid.NewGuid(), 100, 30)).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateStockForecastCommand(Guid.Empty, 100, 30)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateIncomeValidatorTests
{
    private readonly UpdateIncomeValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateIncomeCommand(Guid.NewGuid(), "Güncel", null)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateIncomeCommand(Guid.Empty, "N", null)).IsValid.Should().BeFalse();
}

#endregion

#region Accounting Update

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateChartOfAccountValidatorTests
{
    private readonly UpdateChartOfAccountValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateChartOfAccountCommand(Guid.NewGuid(), Guid.NewGuid(), "120", "Alıcılar")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateChartOfAccountCommand(Guid.Empty, Guid.NewGuid(), "120", "A")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCounterpartyValidatorTests
{
    private readonly UpdateCounterpartyValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateCounterpartyCommand(Guid.NewGuid(), Guid.NewGuid(), "ABC Ltd.")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateCounterpartyCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateFixedAssetValidatorTests
{
    private readonly UpdateFixedAssetValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateFixedAssetCommand(Guid.NewGuid(), Guid.NewGuid(), "Yazıcı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateFixedAssetCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateFixedExpenseValidatorTests
{
    private readonly UpdateFixedExpenseValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateFixedExpenseCommand(Guid.NewGuid(), Guid.NewGuid(), "Kira", 15000m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateFixedExpenseCommand(Guid.Empty, Guid.NewGuid(), "N", 0m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePenaltyRecordValidatorTests
{
    private readonly UpdatePenaltyRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePenaltyRecordCommand(Guid.NewGuid(), Guid.NewGuid(), "Güncelleme")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdatePenaltyRecordCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePlatformCommissionRateValidatorTests
{
    private readonly UpdatePlatformCommissionRateValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePlatformCommissionRateCommand(Guid.NewGuid(), Guid.NewGuid(), 12.5m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdatePlatformCommissionRateCommand(Guid.Empty, Guid.NewGuid(), 0m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateSalaryRecordValidatorTests
{
    private readonly UpdateSalaryRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateSalaryRecordCommand(Guid.NewGuid(), Guid.NewGuid(), "Düzeltme")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateSalaryRecordCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateTaxRecordValidatorTests
{
    private readonly UpdateTaxRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateTaxRecordCommand(Guid.NewGuid(), Guid.NewGuid(), "KDV düzeltme")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateTaxRecordCommand(Guid.Empty, Guid.NewGuid(), "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordCargoExpenseValidatorTests
{
    private readonly RecordCargoExpenseValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RecordCargoExpenseCommand(Guid.NewGuid(), Guid.NewGuid(), 150m, "Yurtiçi")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RecordCargoExpenseCommand(Guid.Empty, Guid.NewGuid(), 0m, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordTaxWithholdingValidatorTests
{
    private readonly RecordTaxWithholdingValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RecordTaxWithholdingCommand(Guid.NewGuid(), 1500m, "KDV Tevkifatı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RecordTaxWithholdingCommand(Guid.Empty, 0m, "N")).IsValid.Should().BeFalse();
}

#endregion

#region Calendar + Settings

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCalendarEventValidatorTests
{
    private readonly UpdateCalendarEventValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateCalendarEventCommand(Guid.NewGuid(), "Toplantı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateCalendarEventCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GenerateTaxCalendarValidatorTests
{
    private readonly GenerateTaxCalendarValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GenerateTaxCalendarCommand(Guid.NewGuid(), 2026)).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GenerateTaxCalendarCommand(Guid.Empty, 2026)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProfileSettingsValidatorTests
{
    private readonly UpdateProfileSettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProfileSettingsCommand(Guid.NewGuid(), "Ali", "ali@test.com")).IsValid.Should().BeTrue();
    [Fact] public void Empty_UserId() => _v.Validate(new UpdateProfileSettingsCommand(Guid.Empty, "N", "e")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateStoreSettingsValidatorTests
{
    private readonly UpdateStoreSettingsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateStoreSettingsCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new UpdateStoreSettingsCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
}

#endregion
