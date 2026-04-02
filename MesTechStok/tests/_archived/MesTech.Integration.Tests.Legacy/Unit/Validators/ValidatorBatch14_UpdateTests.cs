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
using MesTech.Domain.Enums;
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
    [Fact] public void Valid() => _v.Validate(new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "Depo A", "WH-01", null, "Standard", true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateWarehouseCommand(Guid.Empty, Guid.Empty, "D", "W", null, "Standard", true)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateProductPriceValidatorTests
{
    private readonly UpdateProductPriceValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateProductPriceCommand { ProductId = Guid.NewGuid(), RecommendedPrice = 99.90m, TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateProductPriceCommand { ProductId = Guid.Empty, RecommendedPrice = 99.90m, TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
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
    [Fact] public void Valid() => _v.Validate(new UpdateProductContentCommand { ProductId = Guid.NewGuid(), GeneratedContent = "Yeni açıklama", AiProvider = "openai", TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateProductContentCommand { ProductId = Guid.Empty, GeneratedContent = "D", AiProvider = "S", TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDocumentCategoryValidatorTests
{
    private readonly UpdateDocumentCategoryValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateDocumentCategoryCommand { DocumentId = Guid.NewGuid(), DocumentType = "Faturalar", Confidence = 0.9m, TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateDocumentCategoryCommand { DocumentId = Guid.Empty, DocumentType = "N", Confidence = 0.5m, TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDocumentMetadataValidatorTests
{
    private readonly UpdateDocumentMetadataValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateDocumentMetadataCommand { DocumentId = Guid.NewGuid(), ProcessedJson = "{}", Confidence = 0.9m, TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateDocumentMetadataCommand { DocumentId = Guid.Empty, ProcessedJson = "f", Confidence = 0.5m, TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateStockForecastValidatorTests
{
    private readonly UpdateStockForecastValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateStockForecastCommand { ProductId = Guid.NewGuid(), PredictedDemand7d = 100, DaysUntilStockout = 30, TenantId = Guid.NewGuid() }).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new UpdateStockForecastCommand { ProductId = Guid.Empty, PredictedDemand7d = 100, DaysUntilStockout = 30, TenantId = Guid.NewGuid() }).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateIncomeValidatorTests
{
    private readonly UpdateIncomeValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateIncomeCommand(Guid.NewGuid(), "Güncel")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateIncomeCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

#endregion

#region Accounting Update

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateChartOfAccountValidatorTests
{
    private readonly UpdateChartOfAccountValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateChartOfAccountCommand(Guid.NewGuid(), "Alıcılar")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateChartOfAccountCommand(Guid.Empty, "A")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCounterpartyValidatorTests
{
    private readonly UpdateCounterpartyValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateCounterpartyCommand(Guid.NewGuid(), "ABC Ltd.")).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateCounterpartyCommand(Guid.Empty, "N")).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateFixedAssetValidatorTests
{
    private readonly UpdateFixedAssetValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateFixedAssetCommand(Guid.NewGuid(), Guid.NewGuid(), "Yazıcı", null, 5)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateFixedAssetCommand(Guid.Empty, Guid.NewGuid(), "N", null, 5)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateFixedExpenseValidatorTests
{
    private readonly UpdateFixedExpenseValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateFixedExpenseCommand(Guid.NewGuid(), 15000m, true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateFixedExpenseCommand(Guid.Empty, 0m, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePenaltyRecordValidatorTests
{
    private readonly UpdatePenaltyRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePenaltyRecordCommand(Guid.NewGuid(), PaymentStatus.Completed)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdatePenaltyRecordCommand(Guid.Empty, PaymentStatus.Pending)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePlatformCommissionRateValidatorTests
{
    private readonly UpdatePlatformCommissionRateValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePlatformCommissionRateCommand(Guid.NewGuid(), 12.5m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdatePlatformCommissionRateCommand(Guid.Empty, 0m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateSalaryRecordValidatorTests
{
    private readonly UpdateSalaryRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateSalaryRecordCommand(Guid.NewGuid(), PaymentStatus.Completed)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateSalaryRecordCommand(Guid.Empty, PaymentStatus.Pending)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateTaxRecordValidatorTests
{
    private readonly UpdateTaxRecordValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateTaxRecordCommand(Guid.NewGuid(), true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateTaxRecordCommand(Guid.Empty, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordCargoExpenseValidatorTests
{
    private readonly RecordCargoExpenseValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RecordCargoExpenseCommand(Guid.NewGuid(), "Yurtiçi", 150m, "ORD-001", "TR123")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RecordCargoExpenseCommand(Guid.Empty, "N", 0m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RecordTaxWithholdingValidatorTests
{
    private readonly RecordTaxWithholdingValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RecordTaxWithholdingCommand(Guid.NewGuid(), 1500m, 0.20m, "KDV Tevkifatı")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new RecordTaxWithholdingCommand(Guid.Empty, 0m, 0m, "N")).IsValid.Should().BeFalse();
}

#endregion

#region Calendar + Settings

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCalendarEventValidatorTests
{
    private readonly UpdateCalendarEventValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateCalendarEventCommand(Guid.NewGuid(), true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateCalendarEventCommand(Guid.Empty, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GenerateTaxCalendarValidatorTests
{
    private readonly GenerateTaxCalendarValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new GenerateTaxCalendarCommand(2026, Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new GenerateTaxCalendarCommand(2026, Guid.Empty)).IsValid.Should().BeFalse();
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
    [Fact] public void Valid() => _v.Validate(new UpdateStoreSettingsCommand(Guid.NewGuid(), "MesTech Store", "1234567890", "555-1234", "info@test.com", "Istanbul")).IsValid.Should().BeTrue();
    [Fact] public void Empty_TenantId() => _v.Validate(new UpdateStoreSettingsCommand(Guid.Empty, "N", null, null, null, null)).IsValid.Should().BeFalse();
}

#endregion
