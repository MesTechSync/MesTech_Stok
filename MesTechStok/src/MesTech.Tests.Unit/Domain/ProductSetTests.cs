using MesTech.Domain.Common;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

public class ProductSetTests
{
    private static ProductSet CreateSet(string name = "Test Set")
        => new() { TenantId = Guid.NewGuid(), Name = name };

    // ── ProductSet.AddItem ──

    [Fact]
    public void AddItem_Should_Add_Item_To_Collection()
    {
        var set = CreateSet();
        var productId = Guid.NewGuid();

        set.AddItem(productId, quantity: 2);

        Assert.Single(set.Items);
        Assert.Equal(productId, set.Items.First().ProductId);
        Assert.Equal(2, set.Items.First().Quantity);
    }

    [Fact]
    public void AddItem_Duplicate_ProductId_Should_Throw_InvalidOperationException()
    {
        var set = CreateSet();
        var productId = Guid.NewGuid();
        set.AddItem(productId, quantity: 1);

        var ex = Assert.Throws<InvalidOperationException>(() => set.AddItem(productId, quantity: 3));
        Assert.Contains("already in set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddItem_EmptyGuid_Should_Throw_ArgumentException()
    {
        var set = CreateSet();

        Assert.Throws<ArgumentException>(() => set.AddItem(Guid.Empty, quantity: 1));
    }

    [Fact]
    public void AddItem_ZeroQuantity_Should_Throw_ArgumentOutOfRangeException()
    {
        var set = CreateSet();

        Assert.Throws<ArgumentOutOfRangeException>(() => set.AddItem(Guid.NewGuid(), quantity: 0));
    }

    // ── ProductSet.RemoveItem ──

    [Fact]
    public void RemoveItem_Should_Remove_Correct_Item()
    {
        var set = CreateSet();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        set.AddItem(productId1, quantity: 1);
        set.AddItem(productId2, quantity: 2);

        set.RemoveItem(productId1);

        Assert.Single(set.Items);
        Assert.Equal(productId2, set.Items.First().ProductId);
    }

    [Fact]
    public void RemoveItem_Nonexistent_Should_Throw_InvalidOperationException()
    {
        var set = CreateSet();
        var nonExistentId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(() => set.RemoveItem(nonExistentId));
        Assert.Contains(nonExistentId.ToString(), ex.Message);
    }

    // ── ProductSet.GetStockDeductions ──

    [Fact]
    public void GetStockDeductions_Should_Return_Correct_Pairs()
    {
        var set = CreateSet();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        set.AddItem(productId1, quantity: 3);
        set.AddItem(productId2, quantity: 5);

        var deductions = set.GetStockDeductions();

        Assert.Equal(2, deductions.Count);
        Assert.Contains(deductions, d => d.ProductId == productId1 && d.Quantity == 3);
        Assert.Contains(deductions, d => d.ProductId == productId2 && d.Quantity == 5);
    }

    [Fact]
    public void GetStockDeductions_TwoItems_Returns_Two_Entries_With_Correct_Quantities()
    {
        var set = CreateSet();
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        set.AddItem(idA, quantity: 10);
        set.AddItem(idB, quantity: 1);

        var deductions = set.GetStockDeductions();

        Assert.Equal(2, deductions.Count);
        Assert.Equal(10, deductions.First(d => d.ProductId == idA).Quantity);
        Assert.Equal(1, deductions.First(d => d.ProductId == idB).Quantity);
    }

    // ── ProductSetItem.Create validation ──

    [Fact]
    public void ProductSetItem_Create_ZeroQuantity_Should_Throw_ArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ProductSetItem.Create(Guid.NewGuid(), Guid.NewGuid(), quantity: 0));
        Assert.Equal("quantity", ex.ParamName);
    }

    [Fact]
    public void ProductSetItem_Create_NegativeQuantity_Should_Throw_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ProductSetItem.Create(Guid.NewGuid(), Guid.NewGuid(), quantity: -5));
    }

    [Fact]
    public void ProductSetItem_Create_EmptyProductId_Should_Throw_ArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ProductSetItem.Create(Guid.NewGuid(), Guid.Empty, quantity: 1));
    }

    [Fact]
    public void ProductSetItem_Create_EmptyProductSetId_Should_Throw_ArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ProductSetItem.Create(Guid.Empty, Guid.NewGuid(), quantity: 1));
    }

    [Fact]
    public void ProductSetItem_Create_ValidArgs_Should_Set_Properties()
    {
        var setId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var item = ProductSetItem.Create(setId, productId, quantity: 4);

        Assert.Equal(setId, item.ProductSetId);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(4, item.Quantity);
    }

    // ── ITenantEntity ──

    [Fact]
    public void ProductSet_Should_Implement_ITenantEntity()
    {
        var set = CreateSet();
        Assert.IsAssignableFrom<ITenantEntity>(set);
    }

    // ── BaseEntity ──

    [Fact]
    public void ProductSet_Should_Inherit_BaseEntity()
    {
        var set = CreateSet();
        Assert.IsAssignableFrom<BaseEntity>(set);
        Assert.NotEqual(Guid.Empty, set.Id);
    }
}
