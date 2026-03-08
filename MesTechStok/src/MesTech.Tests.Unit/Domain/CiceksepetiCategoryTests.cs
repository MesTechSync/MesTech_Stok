using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class CiceksepetiCategoryTests
{
    [Fact]
    public void CiceksepetiCategory_ShouldInheritBaseEntity()
    {
        var cat = new CiceksepetiCategory
        {
            CiceksepetiCategoryId = 12345,
            CategoryName = "Elektronik",
            IsLeaf = false
        };

        cat.Id.Should().NotBeEmpty();
        cat.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        cat.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CiceksepetiCategory_Leaf_ShouldBeTrue()
    {
        var cat = new CiceksepetiCategory
        {
            CiceksepetiCategoryId = 67890,
            CategoryName = "Cep Telefonu",
            ParentCategoryId = 12345,
            IsLeaf = true
        };

        cat.IsLeaf.Should().BeTrue();
        cat.ParentCategoryId.Should().Be(12345);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var cat = new CiceksepetiCategory { CiceksepetiCategoryId = 100, CategoryName = "Test" };
        cat.ToString().Should().Be("[CS-100] Test");
    }
}
