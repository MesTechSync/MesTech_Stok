using FluentAssertions;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Commands.UpdateCategory;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region AdjustStock

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AdjustStockValidatorTests
{
    private readonly AdjustStockValidator _validator = new();

    private static AdjustStockCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        Quantity: -5,
        Reason: "Sayım farkı düzeltme",
        PerformedBy: "admin@mestech.com");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Zero_Quantity_Fails()
    {
        var cmd = ValidCommand() with { Quantity = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Positive_Quantity_Passes()
    {
        var cmd = ValidCommand() with { Quantity = 10 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Negative_Quantity_Passes()
    {
        var cmd = ValidCommand() with { Quantity = -3 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with { Reason = null, PerformedBy = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reason_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

#endregion

#region BulkUpdateStock

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BulkUpdateStockValidatorTests
{
    private readonly BulkUpdateStockValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new BulkUpdateStockCommand(
            Items: new List<BulkUpdateStockItem> { new(Guid.NewGuid(), 50) },
            TenantId: Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new BulkUpdateStockCommand(
            Items: new List<BulkUpdateStockItem> { new(Guid.NewGuid(), 10) },
            TenantId: Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Null_TenantId_Fails()
    {
        var cmd = new BulkUpdateStockCommand(
            Items: new List<BulkUpdateStockItem> { new(Guid.NewGuid(), 10) },
            TenantId: null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region CreateCategory

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateCategoryCommand("Elektronik", "CAT-001");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = new CreateCategoryCommand("", "CAT-001");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over_500_Chars_Fails()
    {
        var cmd = new CreateCategoryCommand(new string('C', 501), "CAT-001");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = new CreateCategoryCommand("Elektronik", "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}

#endregion

#region UpdateCariHesap

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCariHesapValidatorTests
{
    private readonly UpdateCariHesapValidator _validator = new();

    private static UpdateCariHesapCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Güncel Firma Adı",
        TaxNumber: "1234567890",
        Type: CariHesapType.Customer,
        Phone: "+90 212 555 0000",
        Email: "info@firma.com",
        Address: "İstanbul");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
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
    public void Invalid_Type_Enum_Fails()
    {
        var cmd = ValidCommand() with { Type = (CariHesapType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            TaxNumber = null, Phone = null, Email = null, Address = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region UpdateCategory

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new UpdateCategoryCommand(Guid.NewGuid(), "Elektronik", "CAT-001", true);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new UpdateCategoryCommand(Guid.Empty, "Elektronik", "CAT-001", true);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = new UpdateCategoryCommand(Guid.NewGuid(), "", "CAT-001", true);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = new UpdateCategoryCommand(Guid.NewGuid(), "Elektronik", "", false);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}

#endregion
