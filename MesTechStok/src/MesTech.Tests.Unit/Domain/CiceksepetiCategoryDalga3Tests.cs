using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Dalga 3 CiceksepetiCategory tests — FakeData helpers, hierarchy, edge cases.
/// Supplements CiceksepetiCategoryTests.cs without duplication.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class CiceksepetiCategoryDalga3Tests
{
    // ── Leaf vs Root distinction ──

    [Fact]
    public void LeafCategory_HasNoChildren_IsLeafTrue()
    {
        var leaf = FakeData.CreateCiceksepetiCategory(
            categoryId: 5001,
            name: "Samsung Galaxy S25",
            parentId: 100,
            isLeaf: true);

        leaf.IsLeaf.Should().BeTrue();
        leaf.ParentCategoryId.Should().Be(100);
        leaf.CiceksepetiCategoryId.Should().Be(5001);
    }

    [Fact]
    public void RootCategory_HasNoParent_ParentIdNull()
    {
        var root = FakeData.CreateCiceksepetiCategory(
            categoryId: 1,
            name: "Ana Kategori",
            parentId: null,
            isLeaf: false);

        root.ParentCategoryId.Should().BeNull();
        root.IsLeaf.Should().BeFalse();
    }

    [Fact]
    public void ParentCategory_HasParentId_Set()
    {
        var sub = FakeData.CreateCiceksepetiCategory(
            categoryId: 200,
            name: "Elektronik > Telefon",
            parentId: 100);

        sub.ParentCategoryId.Should().Be(100);
        sub.ParentCategoryId.Should().NotBeNull();
    }

    // ── Default values ──

    [Fact]
    public void DefaultCategory_NameIsEmpty_IsLeafFalse()
    {
        var cat = new CiceksepetiCategory();

        cat.CategoryName.Should().BeEmpty();
        cat.IsLeaf.Should().BeFalse();
        cat.ParentCategoryId.Should().BeNull();
        cat.CiceksepetiCategoryId.Should().Be(0);
    }

    // ── FakeData helper ──

    [Fact]
    public void FakeData_CreateCiceksepetiCategory_SetsCorrectFields()
    {
        var cat = FakeData.CreateCiceksepetiCategory(
            categoryId: 42,
            name: "Cicek",
            parentId: 10,
            isLeaf: true);

        cat.CiceksepetiCategoryId.Should().Be(42);
        cat.CategoryName.Should().Be("Cicek");
        cat.ParentCategoryId.Should().Be(10);
        cat.IsLeaf.Should().BeTrue();
        cat.Id.Should().NotBeEmpty(); // BaseEntity
        cat.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FakeData_CreateCiceksepetiCategory_DefaultParams_GeneratesRandomValues()
    {
        var cat = FakeData.CreateCiceksepetiCategory();

        cat.CiceksepetiCategoryId.Should().BeInRange(1000, 99999);
        cat.CategoryName.Should().NotBeNullOrWhiteSpace();
        cat.ParentCategoryId.Should().BeNull();
        cat.IsLeaf.Should().BeFalse();
    }

    // ── ToString ──

    [Fact]
    public void ToString_LeafCategory_FormatsCorrectly()
    {
        var cat = FakeData.CreateCiceksepetiCategory(
            categoryId: 9999,
            name: "Orkide");

        cat.ToString().Should().Be("[CS-9999] Orkide");
    }

    // ── Multiple categories (hierarchy scenario) ──

    [Fact]
    public void CategoryHierarchy_ThreeLevels_ParentChainCorrect()
    {
        var root = FakeData.CreateCiceksepetiCategory(categoryId: 1, name: "Root", parentId: null, isLeaf: false);
        var mid = FakeData.CreateCiceksepetiCategory(categoryId: 10, name: "Orta", parentId: 1, isLeaf: false);
        var leaf = FakeData.CreateCiceksepetiCategory(categoryId: 100, name: "Yaprak", parentId: 10, isLeaf: true);

        root.ParentCategoryId.Should().BeNull();
        mid.ParentCategoryId.Should().Be(root.CiceksepetiCategoryId);
        leaf.ParentCategoryId.Should().Be(mid.CiceksepetiCategoryId);
        leaf.IsLeaf.Should().BeTrue();
        root.IsLeaf.Should().BeFalse();
        mid.IsLeaf.Should().BeFalse();
    }

    // ── Soft delete (inherited from BaseEntity) ──

    [Fact]
    public void CiceksepetiCategory_SoftDelete_DefaultsFalse()
    {
        var cat = FakeData.CreateCiceksepetiCategory();

        cat.IsDeleted.Should().BeFalse();
        cat.DeletedAt.Should().BeNull();
        cat.DeletedBy.Should().BeNull();
    }
}
