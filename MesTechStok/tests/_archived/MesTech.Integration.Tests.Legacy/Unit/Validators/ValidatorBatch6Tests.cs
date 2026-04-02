using FluentAssertions;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.FinalizeErpReconciliation;
using MesTech.Application.Commands.MarkNotificationDelivered;
using MesTech.Application.Commands.ProcessAiRecommendation;
using MesTech.Application.Commands.ProcessBotInvoiceRequest;
using MesTech.Application.Commands.ProcessBotReturnRequest;
using MesTech.Application.Commands.PushOrderToBitrix24;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region CreateQuotation

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateQuotationValidatorTests
{
    private readonly CreateQuotationValidator _validator = new();

    private static CreateQuotationCommand ValidCommand() => new(
        QuotationNumber: "TEK-2026-001",
        ValidUntil: DateTime.UtcNow.AddDays(30),
        CustomerId: Guid.NewGuid(),
        CustomerName: "ABC Ticaret",
        CustomerTaxNumber: "1234567890",
        CustomerTaxOffice: "Kadıköy VD",
        CustomerAddress: "İstanbul",
        CustomerEmail: "info@abc.com",
        Notes: "Fiyat teklifi",
        Terms: "30 gün vade");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_QuotationNumber_Fails()
    {
        var cmd = ValidCommand() with { QuotationNumber = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public void QuotationNumber_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { QuotationNumber = new string('Q', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public void Empty_CustomerName_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            CustomerTaxNumber = null,
            CustomerTaxOffice = null,
            CustomerAddress = null,
            CustomerEmail = null,
            Notes = null,
            Terms = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateSupplier

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateSupplierValidatorTests
{
    private readonly CreateSupplierValidator _validator = new();

    private static CreateSupplierCommand ValidCommand() => new(
        Name: "Toptan Tedarik A.Ş.",
        Code: "SUP-001",
        ContactPerson: "Mehmet Bey",
        Email: "info@toptan.com",
        Phone: "+90 212 555 0000",
        City: "İstanbul",
        TaxNumber: "9876543210",
        TaxOffice: "Beşiktaş VD");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
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
    public void Name_Over_200_Chars_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('S', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var cmd = ValidCommand() with { Email = "not-email" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            ContactPerson = null, Email = null, Phone = null,
            City = null, TaxNumber = null, TaxOffice = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateWarehouse

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateWarehouseValidatorTests
{
    private readonly CreateWarehouseValidator _validator = new();

    private static CreateWarehouseCommand ValidCommand() => new(
        Name: "Ana Depo",
        Code: "WH-001",
        Address: "Tuzla Organize Sanayi",
        City: "İstanbul",
        IsDefault: true,
        TenantId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
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
    public void Empty_Code_Fails()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
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
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { Address = null, City = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region FinalizeErpReconciliation

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class FinalizeErpReconciliationValidatorTests
{
    private readonly FinalizeErpReconciliationValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new FinalizeErpReconciliationCommand
        {
            ErpProvider = "Parasut",
            ReconciledCount = 150,
            MismatchCount = 3,
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ErpProvider_Fails()
    {
        var cmd = new FinalizeErpReconciliationCommand
        {
            ErpProvider = "",
            ReconciledCount = 10,
            MismatchCount = 0,
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new FinalizeErpReconciliationCommand
        {
            ErpProvider = "Parasut",
            ReconciledCount = 10,
            MismatchCount = 0,
            TenantId = Guid.Empty
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region MarkNotificationDelivered

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MarkNotificationDeliveredValidatorTests
{
    private readonly MarkNotificationDeliveredValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new MarkNotificationDeliveredCommand
        {
            TenantId = Guid.NewGuid(),
            Channel = "Email",
            Recipient = "user@example.com",
            TemplateName = "OrderConfirmation",
            Content = "Sipariş onaylandı",
            Success = true,
            ErrorMessage = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new MarkNotificationDeliveredCommand
        {
            TenantId = Guid.Empty,
            Channel = "SMS",
            Recipient = "+905551234567",
            TemplateName = "OTP",
            Content = "Kodunuz: 1234",
            Success = true
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Channel_Fails()
    {
        var cmd = new MarkNotificationDeliveredCommand
        {
            TenantId = Guid.NewGuid(),
            Channel = "",
            Recipient = "user@test.com",
            TemplateName = "Test",
            Content = "Test",
            Success = true
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public void Empty_Recipient_Fails()
    {
        var cmd = new MarkNotificationDeliveredCommand
        {
            TenantId = Guid.NewGuid(),
            Channel = "Email",
            Recipient = "",
            TemplateName = "Test",
            Content = "Test",
            Success = false
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public void Empty_Content_Fails()
    {
        var cmd = new MarkNotificationDeliveredCommand
        {
            TenantId = Guid.NewGuid(),
            Channel = "Push",
            Recipient = "device-token",
            TemplateName = "Alert",
            Content = "",
            Success = true
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }
}

#endregion

#region ProcessAiRecommendation

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ProcessAiRecommendationValidatorTests
{
    private readonly ProcessAiRecommendationValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ProcessAiRecommendationCommand
        {
            RecommendationType = "PriceOptimization",
            Title = "Fiyat düşürme önerisi",
            Description = "SKU-001 için %5 indirim öneriliyor",
            ActionUrl = "/products/SKU-001/pricing",
            Priority = "High",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RecommendationType_Fails()
    {
        var cmd = new ProcessAiRecommendationCommand
        {
            RecommendationType = "",
            Title = "Test",
            Description = "Test",
            Priority = "Low",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecommendationType");
    }

    [Fact]
    public void Empty_Title_Fails()
    {
        var cmd = new ProcessAiRecommendationCommand
        {
            RecommendationType = "StockAlert",
            Title = "",
            Description = "Stok azaldı",
            Priority = "Medium",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new ProcessAiRecommendationCommand
        {
            RecommendationType = "Test",
            Title = "Test",
            Description = "Test",
            Priority = "Low",
            TenantId = Guid.Empty
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region ProcessBotInvoiceRequest

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ProcessBotInvoiceRequestValidatorTests
{
    private readonly ProcessBotInvoiceRequestValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ProcessBotInvoiceRequestCommand
        {
            CustomerPhone = "+905551234567",
            OrderNumber = "ORD-2026-001",
            RequestChannel = "WhatsApp",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CustomerPhone_Fails()
    {
        var cmd = new ProcessBotInvoiceRequestCommand
        {
            CustomerPhone = "",
            OrderNumber = "ORD-001",
            RequestChannel = "Telegram",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerPhone");
    }

    [Fact]
    public void Empty_OrderNumber_Fails()
    {
        var cmd = new ProcessBotInvoiceRequestCommand
        {
            CustomerPhone = "+905550000000",
            OrderNumber = "",
            RequestChannel = "WhatsApp",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new ProcessBotInvoiceRequestCommand
        {
            CustomerPhone = "+905550000000",
            OrderNumber = "ORD-001",
            RequestChannel = "Bot",
            TenantId = Guid.Empty
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region ProcessBotReturnRequest

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ProcessBotReturnRequestValidatorTests
{
    private readonly ProcessBotReturnRequestValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ProcessBotReturnRequestCommand
        {
            CustomerPhone = "+905559876543",
            OrderNumber = "ORD-2026-050",
            ReturnReason = "Ürün hasarlı geldi",
            RequestChannel = "WhatsApp",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CustomerPhone_Fails()
    {
        var cmd = new ProcessBotReturnRequestCommand
        {
            CustomerPhone = "",
            OrderNumber = "ORD-001",
            RequestChannel = "Telegram",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerPhone");
    }

    [Fact]
    public void Empty_OrderNumber_Fails()
    {
        var cmd = new ProcessBotReturnRequestCommand
        {
            CustomerPhone = "+905550000000",
            OrderNumber = "",
            RequestChannel = "Bot",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public void Null_ReturnReason_Passes()
    {
        var cmd = new ProcessBotReturnRequestCommand
        {
            CustomerPhone = "+905550000000",
            OrderNumber = "ORD-001",
            ReturnReason = null,
            RequestChannel = "WhatsApp",
            TenantId = Guid.NewGuid()
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new ProcessBotReturnRequestCommand
        {
            CustomerPhone = "+905550000000",
            OrderNumber = "ORD-001",
            RequestChannel = "Bot",
            TenantId = Guid.Empty
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region PushOrderToBitrix24

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class PushOrderToBitrix24ValidatorTests
{
    private readonly PushOrderToBitrix24Validator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new PushOrderToBitrix24Command(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_OrderId_Fails()
    {
        var cmd = new PushOrderToBitrix24Command(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }
}

#endregion
