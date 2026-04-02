using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region ChangeSubscriptionPlan

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ChangeSubscriptionPlanValidatorTests
{
    private readonly ChangeSubscriptionPlanValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_NewPlanId_Fails()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPlanId");
    }
}

#endregion

#region CloseCashRegister

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CloseCashRegisterValidatorTests
{
    private readonly CloseCashRegisterValidator _validator = new();

    private static CloseCashRegisterCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CashRegisterId: Guid.NewGuid(),
        ClosingDate: DateTime.UtcNow,
        ActualCashAmount: 5000m);

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
    public void Empty_CashRegisterId_Fails()
    {
        var cmd = ValidCommand() with { CashRegisterId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CashRegisterId");
    }

    [Fact]
    public void Negative_ActualCashAmount_Fails()
    {
        var cmd = ValidCommand() with { ActualCashAmount = -100m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ActualCashAmount");
    }

    [Fact]
    public void Zero_ActualCashAmount_Passes()
    {
        var cmd = ValidCommand() with { ActualCashAmount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CompleteOnboardingStep

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CompleteOnboardingStepValidatorTests
{
    private readonly CompleteOnboardingStepValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CompleteOnboardingStepCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CompleteOnboardingStepCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region CompleteTask

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CompleteTaskValidatorTests
{
    private readonly CompleteTaskValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TaskId_Fails()
    {
        var cmd = new CompleteTaskCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaskId");
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = new CompleteTaskCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}

#endregion

#region ConvertQuotationToInvoice

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ConvertQuotationToInvoiceValidatorTests
{
    private readonly ConvertQuotationToInvoiceValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ConvertQuotationToInvoiceCommand(Guid.NewGuid(), "INV-2026-001");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_QuotationId_Fails()
    {
        var cmd = new ConvertQuotationToInvoiceCommand(Guid.Empty, "INV-001");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationId");
    }

    [Fact]
    public void Empty_InvoiceNumber_Fails()
    {
        var cmd = new ConvertQuotationToInvoiceCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceNumber");
    }

    [Fact]
    public void InvoiceNumber_Over_500_Chars_Fails()
    {
        var cmd = new ConvertQuotationToInvoiceCommand(Guid.NewGuid(), new string('I', 501));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceNumber");
    }
}

#endregion

#region CreateBaBsRecord

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateBaBsRecordValidatorTests
{
    private readonly CreateBaBsRecordValidator _validator = new();

    private static CreateBaBsRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Type: BaBsType.Ba,
        CounterpartyVkn: "1234567890",
        CounterpartyName: "ABC Ticaret",
        TotalAmount: 50000m,
        DocumentCount: 12);

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
    public void Invalid_Type_Enum_Fails()
    {
        var cmd = ValidCommand() with { Type = (BaBsType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void Empty_CounterpartyVkn_Fails()
    {
        var cmd = ValidCommand() with { CounterpartyVkn = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyVkn");
    }

    [Fact]
    public void Empty_CounterpartyName_Fails()
    {
        var cmd = ValidCommand() with { CounterpartyName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CounterpartyName");
    }

    [Fact]
    public void Negative_TotalAmount_Fails()
    {
        var cmd = ValidCommand() with { TotalAmount = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalAmount");
    }

    [Fact]
    public void Negative_DocumentCount_Fails()
    {
        var cmd = ValidCommand() with { DocumentCount = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentCount");
    }
}

#endregion

#region CreateBarcodeScanLog

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateBarcodeScanLogValidatorTests
{
    private readonly CreateBarcodeScanLogValidator _validator = new();

    private static CreateBarcodeScanLogCommand ValidCommand() => new(
        Barcode: "8690000000001",
        Format: "EAN13",
        Source: "HandScanner",
        DeviceId: "SCANNER-01",
        ValidationMessage: null,
        CorrelationId: null);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Barcode_Fails()
    {
        var cmd = ValidCommand() with { Barcode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Empty_Format_Fails()
    {
        var cmd = ValidCommand() with { Format = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public void Empty_Source_Fails()
    {
        var cmd = ValidCommand() with { Source = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { DeviceId = null, ValidationMessage = null, CorrelationId = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateBillingInvoice

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateBillingInvoiceValidatorTests
{
    private readonly CreateBillingInvoiceValidator _validator = new();

    private static CreateBillingInvoiceCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        SubscriptionId: Guid.NewGuid(),
        Amount: 299.99m,
        CurrencyCode: "TRY",
        TaxRate: 0.20m,
        DueDays: 30);

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
    public void Empty_SubscriptionId_Fails()
    {
        var cmd = ValidCommand() with { SubscriptionId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubscriptionId");
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Empty_CurrencyCode_Fails()
    {
        var cmd = ValidCommand() with { CurrencyCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void CurrencyCode_Not_3_Chars_Fails()
    {
        var cmd = ValidCommand() with { CurrencyCode = "TR" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void TaxRate_Over_1_Fails()
    {
        var cmd = ValidCommand() with { TaxRate = 1.5m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public void Negative_TaxRate_Fails()
    {
        var cmd = ValidCommand() with { TaxRate = -0.1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public void Zero_DueDays_Fails()
    {
        var cmd = ValidCommand() with { DueDays = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDays");
    }
}

#endregion

#region CreateBulkProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateBulkProductsValidatorTests
{
    private readonly CreateBulkProductsValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateBulkProductsCommand(50);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Zero_Count_Passes()
    {
        var cmd = new CreateBulkProductsCommand(0);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_Count_Fails()
    {
        var cmd = new CreateBulkProductsCommand(-1);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Count");
    }
}

#endregion

#region CreateCalendarEvent

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCalendarEventValidatorTests
{
    private readonly CreateCalendarEventValidator _validator = new();

    private static CreateCalendarEventCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Sprint Review",
        Type: CalendarEventType.Meeting,
        StartAt: DateTime.UtcNow.AddHours(1),
        EndAt: DateTime.UtcNow.AddHours(2),
        IsAllDay: false,
        Description: "Dalga 15 sprint review",
        Location: "Online");

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
    }

    [Fact]
    public void Title_Over_300_Chars_Fails()
    {
        var cmd = ValidCommand() with { Title = new string('T', 301) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_Type_Enum_Fails()
    {
        var cmd = ValidCommand() with { Type = (CalendarEventType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void EndAt_Before_StartAt_NonAllDay_Fails()
    {
        var now = DateTime.UtcNow;
        var cmd = ValidCommand() with { StartAt = now.AddHours(2), EndAt = now.AddHours(1), IsAllDay = false };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AllDay_Event_With_Any_Times_Passes()
    {
        var now = DateTime.UtcNow;
        var cmd = ValidCommand() with { StartAt = now.AddHours(2), EndAt = now.AddHours(1), IsAllDay = true };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { Description = null, Location = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Description_Over_2000_Chars_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 2001) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion
