using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Accounting.Extensions;

[Trait("Category", "Unit")]
public class ExistingEntityExtensionTests
{
    [Fact]
    public void Customer_CurrentBalance_Default_ShouldBeZero()
    {
        var customer = new Customer();

        customer.CurrentBalance.Should().Be(0m);
    }

    [Fact]
    public void Customer_CreditLimit_NullableDefault_ShouldBeNull()
    {
        var customer = new Customer();

        customer.CreditLimit.Should().BeNull();
    }

    [Fact]
    public void Customer_CurrentBalance_SetAndRead_ShouldWork()
    {
        var customer = new Customer { CurrentBalance = 1500.50m };

        customer.CurrentBalance.Should().Be(1500.50m);
    }

    [Fact]
    public void Customer_CreditLimit_SetAndRead_ShouldWork()
    {
        var customer = new Customer { CreditLimit = 10000m };

        customer.CreditLimit.Should().Be(10000m);
    }

    [Fact]
    public void Order_CommissionAmount_NullableDefault_ShouldBeNull()
    {
        var order = FakeData.CreateOrder();

        order.CommissionAmount.Should().BeNull();
    }

    [Fact]
    public void Order_CommissionRate_NullableDefault_ShouldBeNull()
    {
        var order = FakeData.CreateOrder();

        order.CommissionRate.Should().BeNull();
    }

    [Fact]
    public void Order_CommissionAmount_SetAndRead_ShouldWork()
    {
        var order = FakeData.CreateOrder();
        order.CommissionAmount = 150m;

        order.CommissionAmount.Should().Be(150m);
    }

    [Fact]
    public void Order_CommissionRate_SetAndRead_ShouldWork()
    {
        var order = FakeData.CreateOrder();
        order.CommissionRate = 0.15m;

        order.CommissionRate.Should().Be(0.15m);
    }

    [Fact]
    public void Store_CurrentAccountBalance_NullableDefault_ShouldBeNull()
    {
        var store = FakeData.CreateStore(Guid.NewGuid());

        store.CurrentAccountBalance.Should().BeNull();
    }

    [Fact]
    public void Store_CurrentAccountBalance_SetAndRead_ShouldWork()
    {
        var store = FakeData.CreateStore(Guid.NewGuid());
        store.CurrentAccountBalance = 5000m;

        store.CurrentAccountBalance.Should().Be(5000m);
    }

    [Fact]
    public void Invoice_GLAccountCode_NullableDefault_ShouldBeNull()
    {
        var invoice = new Invoice();

        invoice.GLAccountCode.Should().BeNull();
    }

    [Fact]
    public void Invoice_SettlementBatchId_NullableDefault_ShouldBeNull()
    {
        var invoice = new Invoice();

        invoice.SettlementBatchId.Should().BeNull();
    }

    [Fact]
    public void Invoice_GLAccountCode_SetAndRead_ShouldWork()
    {
        var invoice = new Invoice { GLAccountCode = "600.01.001" };

        invoice.GLAccountCode.Should().Be("600.01.001");
    }

    [Fact]
    public void Invoice_SettlementBatchId_SetAndRead_ShouldWork()
    {
        var batchId = Guid.NewGuid();
        var invoice = new Invoice { SettlementBatchId = batchId };

        invoice.SettlementBatchId.Should().Be(batchId);
    }

    [Fact]
    public void Supplier_CreditLimit_NullableDefault_ShouldBeNull()
    {
        var supplier = new Supplier();

        supplier.CreditLimit.Should().BeNull();
    }

    [Fact]
    public void Supplier_CreditLimit_SetAndRead_ShouldWork()
    {
        var supplier = new Supplier { CreditLimit = 25000m };

        supplier.CreditLimit.Should().Be(25000m);
    }

    [Fact]
    public void Order_CommissionFields_SetToZero_ShouldNotBeNull()
    {
        var order = FakeData.CreateOrder();
        order.CommissionAmount = 0m;
        order.CommissionRate = 0m;

        order.CommissionAmount.Should().Be(0m);
        order.CommissionRate.Should().Be(0m);
    }

    [Fact]
    public void Customer_CurrentBalance_SetToNegative_ShouldWork()
    {
        var customer = new Customer { CurrentBalance = -500m };

        customer.CurrentBalance.Should().Be(-500m);
    }

    [Fact]
    public void Store_CurrentAccountBalance_SetToNegative_ShouldWork()
    {
        var store = FakeData.CreateStore(Guid.NewGuid());
        store.CurrentAccountBalance = -1000m;

        store.CurrentAccountBalance.Should().Be(-1000m);
    }

    [Fact]
    public void Invoice_SettlementBatchId_ResetToNull_ShouldWork()
    {
        var invoice = new Invoice { SettlementBatchId = Guid.NewGuid() };
        invoice.SettlementBatchId = null;

        invoice.SettlementBatchId.Should().BeNull();
    }
}
