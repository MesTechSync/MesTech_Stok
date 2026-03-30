using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Commands.CreateEInvoiceFromDraft;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region CancelEInvoice

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CancelEInvoiceValidatorTests
{
    private readonly CancelEInvoiceValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "Hatalı fatura düzeltme");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_EInvoiceId_Fails()
    {
        var cmd = new CancelEInvoiceCommand(Guid.Empty, "Sebep");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EInvoiceId");
    }

    [Fact]
    public void Empty_Reason_Fails()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Reason_Over_500_Chars_Fails()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), new string('R', 501));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

#endregion

#region CancelSubscription

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CancelSubscriptionValidatorTests
{
    private readonly CancelSubscriptionValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "Artık kullanmıyorum");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CancelSubscriptionCommand(Guid.Empty, Guid.NewGuid(), null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_SubscriptionId_Fails()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.Empty, null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubscriptionId");
    }

    [Fact]
    public void Null_Reason_Passes()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reason_Over_500_Chars_Fails()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('X', 501));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

#endregion

#region CreateEInvoiceFromDraft

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateEInvoiceFromDraftValidatorTests
{
    private readonly CreateEInvoiceFromDraftValidator _validator = new();

    private static CreateEInvoiceFromDraftCommand ValidCommand() => new(
        OrderId: Guid.NewGuid(),
        SuggestedEttnNo: "ETTN-2026-00001",
        TenantId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_OrderId_Fails()
    {
        var cmd = ValidCommand() with { OrderId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Empty_SuggestedEttnNo_Fails()
    {
        var cmd = ValidCommand() with { SuggestedEttnNo = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuggestedEttnNo");
    }

    [Fact]
    public void SuggestedEttnNo_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { SuggestedEttnNo = new string('E', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuggestedEttnNo");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region CreateLead

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateLeadValidatorTests
{
    private readonly CreateLeadValidator _validator = new();

    private static CreateLeadCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        FullName: "Ali Veli",
        Email: "ali@example.com",
        Phone: "+90 555 000 1234",
        Company: "ABC Ltd.");

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
    public void Empty_FullName_Fails()
    {
        var cmd = ValidCommand() with { FullName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public void FullName_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { FullName = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { Email = null, Phone = null, Company = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateDeal

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateDealValidatorTests
{
    private readonly CreateDealValidator _validator = new();

    private static CreateDealCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Yeni Anlaşma",
        PipelineId: Guid.NewGuid(),
        StageId: Guid.NewGuid(),
        Amount: 25000m);

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
    public void Empty_Title_Fails()
    {
        var cmd = ValidCommand() with { Title = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Empty_PipelineId_Fails()
    {
        var cmd = ValidCommand() with { PipelineId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PipelineId");
    }

    [Fact]
    public void Empty_StageId_Fails()
    {
        var cmd = ValidCommand() with { StageId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StageId");
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -100m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Zero_Amount_Passes()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateStore

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateStoreValidatorTests
{
    private readonly CreateStoreValidator _validator = new();

    private static CreateStoreCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreName: "MesTech Trendyol Mağaza",
        PlatformType: PlatformType.Trendyol);

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
    public void Empty_StoreName_Fails()
    {
        var cmd = ValidCommand() with { StoreName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreName");
    }

    [Fact]
    public void Invalid_PlatformType_Enum_Fails()
    {
        var cmd = ValidCommand() with { PlatformType = (PlatformType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformType");
    }
}

#endregion

#region CreateProject

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    private static CreateProjectCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Stok Entegrasyonu",
        Description: "Trendyol stok senkronizasyon projesi",
        Color: "#3498DB");

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
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { Description = null, Color = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateInboundShipment

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateInboundShipmentValidatorTests
{
    private readonly CreateInboundShipmentValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateInboundShipmentCommand("FBA-2026-001", "İlk sevkiyat notu");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ShipmentName_Fails()
    {
        var cmd = new CreateInboundShipmentCommand("", "Not");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentName");
    }

    [Fact]
    public void ShipmentName_Over_500_Chars_Fails()
    {
        var cmd = new CreateInboundShipmentCommand(new string('S', 501), null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentName");
    }

    [Fact]
    public void Null_Notes_Passes()
    {
        var cmd = new CreateInboundShipmentCommand("FBA-001", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateDropshipSupplier

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateDropshipSupplierValidatorTests
{
    private readonly CreateDropshipSupplierValidator _validator = new();

    private static CreateDropshipSupplierCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Toptan Tedarikçi A.Ş.",
        WebsiteUrl: "https://tedarikci.com",
        MarkupType: MarkupType.Percentage);

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
    public void Invalid_MarkupType_Enum_Fails()
    {
        var cmd = ValidCommand() with { MarkupType = (MarkupType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MarkupType");
    }

    [Fact]
    public void Null_WebsiteUrl_Passes()
    {
        var cmd = ValidCommand() with { WebsiteUrl = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateFixedAsset

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateFixedAssetValidatorTests
{
    private readonly CreateFixedAssetValidator _validator = new();

    private static CreateFixedAssetCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Barkod Yazıcı",
        AssetCode: "FA-2026-001",
        AcquisitionCost: 15000m,
        Description: "Zebra ZD421 termal barkod yazıcı");

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
    public void Empty_AssetCode_Fails()
    {
        var cmd = ValidCommand() with { AssetCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssetCode");
    }

    [Fact]
    public void Negative_AcquisitionCost_Fails()
    {
        var cmd = ValidCommand() with { AcquisitionCost = -500m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AcquisitionCost");
    }

    [Fact]
    public void Zero_AcquisitionCost_Passes()
    {
        var cmd = ValidCommand() with { AcquisitionCost = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Description_Passes()
    {
        var cmd = ValidCommand() with { Description = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion
