using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Finance.Commands.CreateCashRegister;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Logging.Commands.CreateLogEntry;
using MesTech.Application.Features.Logging.Commands.CleanOldLogs;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region CreateCampaign

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCampaignValidatorTests
{
    private readonly CreateCampaignValidator _validator = new();

    private static CreateCampaignCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Yaz İndirimi",
        StartDate: DateTime.UtcNow,
        EndDate: DateTime.UtcNow.AddDays(30),
        DiscountPercent: 15m);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over_300_Chars_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('K', 301) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void DiscountPercent_Over_100_Fails()
    {
        var cmd = ValidCommand() with { DiscountPercent = 101m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiscountPercent");
    }

    [Fact]
    public void DiscountPercent_Zero_Fails()
    {
        var cmd = ValidCommand() with { DiscountPercent = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiscountPercent");
    }

    [Fact]
    public void EndDate_Before_StartDate_Fails()
    {
        var now = DateTime.UtcNow;
        var cmd = ValidCommand() with { StartDate = now.AddDays(10), EndDate = now };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }
}

#endregion

#region CreateCampaignCommand (second validator)

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCampaignCommandValidatorTests
{
    private readonly CreateCampaignCommandValidator _validator = new();

    private static CreateCampaignCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Kış Kampanyası",
        StartDate: DateTime.UtcNow,
        EndDate: DateTime.UtcNow.AddDays(14),
        DiscountPercent: 25m);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void StartDate_After_EndDate_Fails()
    {
        var now = DateTime.UtcNow;
        var cmd = ValidCommand() with { StartDate = now.AddDays(15), EndDate = now };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartDate");
    }

    [Fact]
    public void DiscountPercent_Boundary_100_Passes()
    {
        var cmd = ValidCommand() with { DiscountPercent = 100m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateCashRegister

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCashRegisterValidatorTests
{
    private readonly CreateCashRegisterValidator _validator = new();

    private static CreateCashRegisterCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Ana Kasa",
        CurrencyCode: "TRY",
        IsDefault: true,
        OpeningBalance: 1000m);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void CurrencyCode_Not_3_Chars_Fails()
    {
        var cmd = ValidCommand() with { CurrencyCode = "US" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void Negative_OpeningBalance_Fails()
    {
        var cmd = ValidCommand() with { OpeningBalance = -500m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OpeningBalance");
    }
}

#endregion

#region CreateErpAccountMapping

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateErpAccountMappingValidatorTests
{
    private readonly CreateErpAccountMappingValidator _validator = new();

    private static CreateErpAccountMappingCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        MesTechCode: "120",
        MesTechName: "Alıcılar",
        MesTechType: "ASSET",
        ErpCode: "120.01",
        ErpName: "Yurtiçi Alıcılar");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_MesTechCode_Fails()
    {
        var cmd = ValidCommand() with { MesTechCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechCode");
    }

    [Fact]
    public void MesTechCode_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { MesTechCode = new string('0', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechCode");
    }

    [Fact]
    public void Empty_ErpCode_Fails()
    {
        var cmd = ValidCommand() with { ErpCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpCode");
    }

    [Fact]
    public void Empty_ErpName_Fails()
    {
        var cmd = ValidCommand() with { ErpName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpName");
    }
}

#endregion

#region CreateFeedSource

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateFeedSourceValidatorTests
{
    private readonly CreateFeedSourceValidator _validator = new();

    private static CreateFeedSourceCommand ValidCommand() => new(
        SupplierId: Guid.NewGuid(),
        Name: "Tedarikçi XML Feed",
        FeedUrl: "https://supplier.com/products.xml",
        Format: FeedFormat.Xml,
        PriceMarkupPercent: 20m,
        PriceMarkupFixed: 5m,
        SyncIntervalMinutes: 60,
        TargetPlatforms: "Trendyol,HepsiBurada",
        AutoDeactivateOnZeroStock: true);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_SupplierId_Fails()
    {
        var cmd = ValidCommand() with { SupplierId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_FeedUrl_Fails()
    {
        var cmd = ValidCommand() with { FeedUrl = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedUrl");
    }

    [Fact]
    public void Negative_PriceMarkupPercent_Fails()
    {
        var cmd = ValidCommand() with { PriceMarkupPercent = -5m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupPercent");
    }

    [Fact]
    public void Negative_PriceMarkupFixed_Fails()
    {
        var cmd = ValidCommand() with { PriceMarkupFixed = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceMarkupFixed");
    }

    [Fact]
    public void Null_TargetPlatforms_Passes()
    {
        var cmd = ValidCommand() with { TargetPlatforms = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateLogEntry

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateLogEntryValidatorTests
{
    private readonly CreateLogEntryValidator _validator = new();

    private static CreateLogEntryCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Level: "Error",
        Category: "OrderProcessing",
        Message: "Sipariş işleme hatası: stok yetersiz");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Invalid_Level_Fails()
    {
        var cmd = ValidCommand() with { Level = "Critical" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Level");
    }

    [Theory]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Debug")]
    public void Valid_Levels_Pass(string level)
    {
        var cmd = ValidCommand() with { Level = level };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Category_Fails()
    {
        var cmd = ValidCommand() with { Category = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }

    [Fact]
    public void Empty_Message_Fails()
    {
        var cmd = ValidCommand() with { Message = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Message");
    }

    [Fact]
    public void Message_Over_4000_Chars_Fails()
    {
        var cmd = ValidCommand() with { Message = new string('M', 4001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Message");
    }
}

#endregion

#region CleanOldLogs

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CleanOldLogsValidatorTests
{
    private readonly CleanOldLogsValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 90);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CleanOldLogsCommand(Guid.Empty, 90);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Zero_DaysToKeep_Fails()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 0);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DaysToKeep");
    }

    [Fact]
    public void DaysToKeep_Over_365_Fails()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 366);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DaysToKeep");
    }

    [Fact]
    public void DaysToKeep_Boundary_365_Passes()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 365);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DaysToKeep_Boundary_1_Passes()
    {
        var cmd = new CleanOldLogsCommand(Guid.NewGuid(), 1);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateSavedReport

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateSavedReportValidatorTests
{
    private readonly CreateSavedReportValidator _validator = new();

    private static CreateSavedReportCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Aylık Satış Raporu",
        ReportType: "SalesReport",
        FilterJson: """{"from":"2026-01-01","to":"2026-01-31"}""",
        CreatedByUserId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_ReportType_Fails()
    {
        var cmd = ValidCommand() with { ReportType = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportType");
    }

    [Fact]
    public void Empty_FilterJson_Fails()
    {
        var cmd = ValidCommand() with { FilterJson = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FilterJson");
    }

    [Fact]
    public void FilterJson_Over_4000_Chars_Fails()
    {
        var cmd = ValidCommand() with { FilterJson = new string('{', 4001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FilterJson");
    }

    [Fact]
    public void Empty_CreatedByUserId_Fails()
    {
        var cmd = ValidCommand() with { CreatedByUserId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreatedByUserId");
    }
}

#endregion

#region BulkUpdateProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BulkUpdateProductsValidatorTests
{
    private readonly BulkUpdateProductsValidator _validator = new();

    private static BulkUpdateProductsCommand ValidCommand() => new(
        ProductIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
        Action: BulkUpdateAction.Activate);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductIds_Fails()
    {
        var cmd = ValidCommand() with { ProductIds = new List<Guid>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductIds");
    }

    [Fact]
    public void Over_500_ProductIds_Fails()
    {
        var ids = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToList();
        var cmd = ValidCommand() with { ProductIds = ids };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductIds");
    }

    [Fact]
    public void Exactly_500_ProductIds_Passes()
    {
        var ids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var cmd = ValidCommand() with { ProductIds = ids };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Action_Enum_Fails()
    {
        var cmd = ValidCommand() with { Action = (BulkUpdateAction)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Action");
    }
}

#endregion

#region CreateAutoOrder

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateAutoOrderValidatorTests
{
    private readonly CreateAutoOrderValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateAutoOrderCommand(
            new List<Guid> { Guid.NewGuid() },
            Guid.NewGuid(),
            AutoApprove: false);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_SupplierId_Fails()
    {
        var cmd = new CreateAutoOrderCommand(
            new List<Guid> { Guid.NewGuid() },
            Guid.Empty,
            AutoApprove: false);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SupplierId");
    }
}

#endregion
