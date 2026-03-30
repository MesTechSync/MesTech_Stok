using FluentAssertions;
using MesTech.Application.Commands.CreateExpense;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Application.Commands.DeleteIncome;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region CreateExpense

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateExpenseValidatorTests
{
    private readonly CreateExpenseValidator _validator = new();

    private static CreateExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        Description: "Kargo masrafı",
        Amount: 250.00m,
        ExpenseType: ExpenseType.Shipping,
        Date: DateTime.UtcNow,
        Note: "Mart ayı kargo");

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
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -50m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Invalid_ExpenseType_Fails()
    {
        var cmd = ValidCommand() with { ExpenseType = (ExpenseType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpenseType");
    }
}

#endregion

#region CreateIncome

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateIncomeValidatorTests
{
    private readonly CreateIncomeValidator _validator = new();

    private static CreateIncomeCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        StoreId: Guid.NewGuid(),
        Description: "Satış geliri",
        Amount: 5000.00m,
        IncomeType: IncomeType.Sales,
        InvoiceId: Guid.NewGuid(),
        Date: DateTime.UtcNow,
        Note: null);

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
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
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
    public void Invalid_IncomeType_Fails()
    {
        var cmd = ValidCommand() with { IncomeType = (IncomeType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IncomeType");
    }
}

#endregion

#region DeleteExpense + DeleteIncome

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteExpenseValidatorTests
{
    private readonly DeleteExpenseValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new DeleteExpenseCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var result = _validator.Validate(new DeleteExpenseCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteIncomeValidatorTests
{
    private readonly DeleteIncomeValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new DeleteIncomeCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var result = _validator.Validate(new DeleteIncomeCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

#endregion

#region UpdateExpense

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateExpenseValidatorTests
{
    private readonly UpdateExpenseValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new UpdateExpenseCommand(Guid.NewGuid(), "Güncel açıklama", "Güncel not");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new UpdateExpenseCommand(Guid.Empty, "Test", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = new UpdateExpenseCommand(Guid.NewGuid(), null, null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Description_Over_500_Chars_Fails()
    {
        var cmd = new UpdateExpenseCommand(Guid.NewGuid(), new string('D', 501), null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}

#endregion

#region CreateFixedExpense

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateFixedExpenseValidatorTests
{
    private readonly CreateFixedExpenseValidator _validator = new();

    private static CreateFixedExpenseCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Ofis kirası",
        MonthlyAmount: 15000m,
        DayOfMonth: 1,
        StartDate: new DateTime(2026, 1, 1),
        Currency: "TRY",
        EndDate: null,
        SupplierName: "Arsa Sahibi",
        SupplierId: null,
        Notes: "Aylık kira");

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
    }

    [Fact]
    public void Zero_MonthlyAmount_Fails()
    {
        var cmd = ValidCommand() with { MonthlyAmount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DayOfMonth_0_Fails()
    {
        var cmd = ValidCommand() with { DayOfMonth = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DayOfMonth_32_Fails()
    {
        var cmd = ValidCommand() with { DayOfMonth = 32 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDate_Before_StartDate_Fails()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDate_After_StartDate_Passes()
    {
        var cmd = ValidCommand() with
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2027, 1, 1)
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region DeleteFixedExpense + DeletePenaltyRecord

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteFixedExpenseValidatorTests
{
    private readonly DeleteFixedExpenseValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new DeleteFixedExpenseCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var result = _validator.Validate(new DeleteFixedExpenseCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeletePenaltyRecordValidatorTests
{
    private readonly DeletePenaltyRecordValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new DeletePenaltyRecordCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var result = _validator.Validate(new DeletePenaltyRecordCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

#endregion

#region CreatePenaltyRecord

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreatePenaltyRecordValidatorTests
{
    private readonly CreatePenaltyRecordValidator _validator = new();

    private static CreatePenaltyRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Source: PenaltySource.Platform,
        Description: "Geç kargo cezası",
        Amount: 500m,
        PenaltyDate: DateTime.UtcNow,
        DueDate: DateTime.UtcNow.AddDays(30),
        ReferenceNumber: "PEN-2026-001",
        RelatedOrderId: Guid.NewGuid(),
        Currency: "TRY",
        Notes: "Trendyol kargo gecikme cezası");

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
    public void Invalid_Source_Enum_Fails()
    {
        var cmd = ValidCommand() with { Source = (PenaltySource)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DueDate_Before_PenaltyDate_Fails()
    {
        var cmd = ValidCommand() with
        {
            PenaltyDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(-5)
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            DueDate = null, ReferenceNumber = null,
            RelatedOrderId = null, Notes = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateSalaryRecord

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateSalaryRecordValidatorTests
{
    private readonly CreateSalaryRecordValidator _validator = new();

    private static CreateSalaryRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        EmployeeName: "Ahmet Yılmaz",
        GrossSalary: 35000m,
        SGKEmployer: 8750m,
        SGKEmployee: 5250m,
        IncomeTax: 5250m,
        StampTax: 266m,
        Year: 2026,
        Month: 3,
        EmployeeId: Guid.NewGuid(),
        Notes: "Mart maaş");

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
    public void Empty_EmployeeName_Fails()
    {
        var cmd = ValidCommand() with { EmployeeName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_GrossSalary_Fails()
    {
        var cmd = ValidCommand() with { GrossSalary = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_SGKEmployer_Fails()
    {
        var cmd = ValidCommand() with { SGKEmployer = -100m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Year_1999_Fails()
    {
        var cmd = ValidCommand() with { Year = 1999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Month_0_Fails()
    {
        var cmd = ValidCommand() with { Month = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Month_13_Fails()
    {
        var cmd = ValidCommand() with { Month = 13 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { EmployeeId = null, Notes = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion
