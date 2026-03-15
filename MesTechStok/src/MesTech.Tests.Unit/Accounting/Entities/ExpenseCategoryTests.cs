using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class ExpenseCategoryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Kirtasiye", "KRT-001");

        cat.Should().NotBeNull();
        cat.Name.Should().Be("Kirtasiye");
        cat.Code.Should().Be("KRT-001");
    }

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ExpenseCategory.Create(_tenantId, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullCode_ShouldAllowNull()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        cat.Code.Should().BeNull();
    }

    [Fact]
    public void Create_WithParentId_ShouldSetParentId()
    {
        var parentId = Guid.NewGuid();
        var cat = ExpenseCategory.Create(_tenantId, "Alt Kategori", parentId: parentId);

        cat.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        cat.Deactivate();

        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");
        cat.Deactivate();

        cat.Activate();

        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateName_ShouldChangeName()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Old Name");

        cat.UpdateName("New Name");

        cat.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrow()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        var act = () => cat.UpdateName("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_ShouldUpdateUpdatedAt()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        cat.UpdateName("Updated");

        cat.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var c1 = ExpenseCategory.Create(_tenantId, "Cat 1");
        var c2 = ExpenseCategory.Create(_tenantId, "Cat 2");

        c1.Id.Should().NotBe(c2.Id);
    }

    [Fact]
    public void Create_NavigationProperty_ShouldBeNull()
    {
        var cat = ExpenseCategory.Create(_tenantId, "Test");

        cat.Parent.Should().BeNull();
    }
}
