using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Commands.ApplyOptimizedPrice;
using MesTech.Application.Commands.ApproveAccountingEntry;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region AcceptQuotation

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AcceptQuotationValidatorTests
{
    private readonly AcceptQuotationValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new AcceptQuotationCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_QuotationId_Fails()
    {
        var cmd = new AcceptQuotationCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationId");
    }
}

#endregion

#region AddProductToPool

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AddProductToPoolValidatorTests
{
    private readonly AddProductToPoolValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), 50.00m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_PoolId_Fails()
    {
        var cmd = new AddProductToPoolCommand(Guid.Empty, Guid.NewGuid(), 50.00m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolId");
    }

    [Fact]
    public void Empty_ProductId_Fails()
    {
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.Empty, 50.00m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Negative_PoolPrice_Fails()
    {
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), -1m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PoolPrice");
    }

    [Fact]
    public void Zero_PoolPrice_Passes()
    {
        var cmd = new AddProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), 0m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region AddStockLot

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AddStockLotValidatorTests
{
    private readonly AddStockLotValidator _validator = new();

    private static AddStockLotCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        LotNumber: "LOT-001",
        Quantity: 100,
        UnitCost: 25m);

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
    public void Empty_LotNumber_Fails()
    {
        var cmd = ValidCommand() with { LotNumber = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public void LotNumber_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { LotNumber = new string('L', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LotNumber");
    }

    [Fact]
    public void Negative_Quantity_Fails()
    {
        var cmd = ValidCommand() with { Quantity = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void Negative_UnitCost_Fails()
    {
        var cmd = ValidCommand() with { UnitCost = -5m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UnitCost");
    }
}

#endregion

#region ApplyOptimizedPrice

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApplyOptimizedPriceValidatorTests
{
    private readonly ApplyOptimizedPriceValidator _validator = new();

    private static ApplyOptimizedPriceCommand ValidCommand() => new(
        ProductId: Guid.NewGuid(),
        SKU: "SKU-PROD-001",
        TenantId: Guid.NewGuid());

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
    public void Empty_SKU_Fails()
    {
        var cmd = ValidCommand() with { SKU = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
    }

    [Fact]
    public void SKU_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { SKU = new string('S', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU");
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

#region ApproveAccountingEntry

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveAccountingEntryValidatorTests
{
    private readonly ApproveAccountingEntryValidator _validator = new();

    private static ApproveAccountingEntryCommand ValidCommand() => new(
        DocumentId: Guid.NewGuid(),
        ApprovedBy: "admin@mestech.com",
        ApprovalSource: "Manual",
        TenantId: Guid.NewGuid());

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DocumentId_Fails()
    {
        var cmd = ValidCommand() with { DocumentId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentId");
    }

    [Fact]
    public void Empty_ApprovedBy_Fails()
    {
        var cmd = ValidCommand() with { ApprovedBy = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovedBy");
    }

    [Fact]
    public void ApprovedBy_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { ApprovedBy = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovedBy");
    }

    [Fact]
    public void Empty_ApprovalSource_Fails()
    {
        var cmd = ValidCommand() with { ApprovalSource = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovalSource");
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

#region ApproveExpense

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveExpenseValidatorTests
{
    private readonly ApproveExpenseValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ApproveExpenseCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ExpenseId_Fails()
    {
        var cmd = new ApproveExpenseCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpenseId");
    }

    [Fact]
    public void Empty_ApproverUserId_Fails()
    {
        var cmd = new ApproveExpenseCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApproverUserId");
    }
}

#endregion

#region ApproveLeave

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveLeaveValidatorTests
{
    private readonly ApproveLeaveValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ApproveLeaveCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_LeaveId_Fails()
    {
        var cmd = new ApproveLeaveCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LeaveId");
    }

    [Fact]
    public void Empty_ApproverUserId_Fails()
    {
        var cmd = new ApproveLeaveCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApproverUserId");
    }
}

#endregion

#region ApproveReturn

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveReturnCommandValidatorTests
{
    private readonly ApproveReturnCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ApproveReturnCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ReturnRequestId_Fails()
    {
        var cmd = new ApproveReturnCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnRequestId");
    }
}

#endregion

#region AutoCompetePrice

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class AutoCompetePriceValidatorTests
{
    private readonly AutoCompetePriceValidator _validator = new();

    private static AutoCompetePriceCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        ProductId: Guid.NewGuid(),
        PlatformCode: "TRENDYOL",
        FloorPrice: 50.00m,
        MaxDiscountPercent: 15m);

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
    public void Empty_ProductId_Fails()
    {
        var cmd = ValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Empty_PlatformCode_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public void PlatformCode_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { PlatformCode = new string('P', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformCode");
    }

    [Fact]
    public void Zero_FloorPrice_Fails()
    {
        var cmd = ValidCommand() with { FloorPrice = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FloorPrice");
    }

    [Fact]
    public void Negative_FloorPrice_Fails()
    {
        var cmd = ValidCommand() with { FloorPrice = -10m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FloorPrice");
    }

    [Fact]
    public void MaxDiscountPercent_Below_Min_Fails()
    {
        var cmd = ValidCommand() with { MaxDiscountPercent = 0.05m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxDiscountPercent");
    }

    [Fact]
    public void MaxDiscountPercent_Above_Max_Fails()
    {
        var cmd = ValidCommand() with { MaxDiscountPercent = 31m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxDiscountPercent");
    }

    [Fact]
    public void MaxDiscountPercent_Boundary_Min_Passes()
    {
        var cmd = ValidCommand() with { MaxDiscountPercent = 0.1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MaxDiscountPercent_Boundary_Max_Passes()
    {
        var cmd = ValidCommand() with { MaxDiscountPercent = 30m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region BatchShipOrders

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BatchShipOrdersValidatorTests
{
    private readonly BatchShipOrdersValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new BatchShipOrdersCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new BatchShipOrdersCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion
