using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Category entity koruma testleri.
/// </summary>
public class CategoryTests
{
    [Fact]
    public void CreateCategory_WithParent_ShouldSetHierarchy()
    {
        var parent = new Category
        {
            Id = 1,
            Name = "Elektronik",
            Code = "ELEC"
        };

        var child = new Category
        {
            Id = 2,
            Name = "Telefon",
            Code = "PHONE",
            ParentCategoryId = parent.Id
        };

        child.ParentCategoryId.Should().Be(1);
        child.Name.Should().Be("Telefon");
    }

    [Fact]
    public void Category_ShouldHaveProductsCollection()
    {
        var category = new Category
        {
            Name = "Test",
            Code = "TST"
        };

        category.Products.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Category_ShouldHaveSubCategoriesCollection()
    {
        var category = new Category
        {
            Name = "Parent",
            Code = "PAR"
        };

        category.SubCategories.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Category_DefaultValues_ShouldBeCorrect()
    {
        var category = new Category();

        category.IsActive.Should().BeTrue();
        category.ShowInMenu.Should().BeTrue();
        category.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Category_ToString_ShouldIncludeCodeAndName()
    {
        var category = new Category { Code = "ELEC", Name = "Elektronik" };

        category.ToString().Should().Contain("ELEC").And.Contain("Elektronik");
    }
}
