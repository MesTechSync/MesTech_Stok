using FluentAssertions;
using MesTech.Domain.Entities.Erp;

namespace MesTech.Tests.Unit.Domain.Erp;

/// <summary>
/// Sprint 4 — ErpFieldMapping entity testleri.
/// Create, UpdateMapping, Deactivate, validasyonlar.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Erp")]
public class ErpFieldMappingTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_Valid_ShouldSucceed()
    {
        var m = ErpFieldMapping.Create(TenantId, "Parasut", "ProductName", "urun_adi",
            isRequired: true, transformExpression: "ToUpper()");

        m.ErpType.Should().Be("Parasut");
        m.MesTechField.Should().Be("ProductName");
        m.ErpField.Should().Be("urun_adi");
        m.IsRequired.Should().BeTrue();
        m.TransformExpression.Should().Be("ToUpper()");
        m.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => ErpFieldMapping.Create(Guid.Empty, "Parasut", "Name", "ad");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyErpType_ShouldThrow()
    {
        var act = () => ErpFieldMapping.Create(TenantId, "", "Name", "ad");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyMesTechField_ShouldThrow()
    {
        var act = () => ErpFieldMapping.Create(TenantId, "Logo", "", "ad");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyErpField_ShouldThrow()
    {
        var act = () => ErpFieldMapping.Create(TenantId, "Logo", "Name", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_DefaultNotRequired()
    {
        var m = ErpFieldMapping.Create(TenantId, "Netsis", "SKU", "stok_kodu");
        m.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void Create_NullTransform_ShouldBeNull()
    {
        var m = ErpFieldMapping.Create(TenantId, "Nebim", "Price", "fiyat");
        m.TransformExpression.Should().BeNull();
    }

    [Fact]
    public void UpdateMapping_ShouldChangeErpField()
    {
        var m = ErpFieldMapping.Create(TenantId, "Parasut", "Stock", "stok");
        m.UpdateMapping("miktar", "Round(2)");

        m.ErpField.Should().Be("miktar");
        m.TransformExpression.Should().Be("Round(2)");
    }

    [Fact]
    public void UpdateMapping_EmptyErpField_ShouldThrow()
    {
        var m = ErpFieldMapping.Create(TenantId, "Parasut", "Stock", "stok");
        var act = () => m.UpdateMapping("", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var m = ErpFieldMapping.Create(TenantId, "ERPNext", "Category", "grup");
        m.IsActive.Should().BeTrue();
        m.Deactivate();

        m.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("Parasut")]
    [InlineData("Logo")]
    [InlineData("Netsis")]
    [InlineData("Nebim")]
    [InlineData("ERPNext")]
    [InlineData("BizimHesap")]
    public void Create_AllErpTypes_ShouldWork(string erpType)
    {
        var m = ErpFieldMapping.Create(TenantId, erpType, "Field", "alan");
        m.ErpType.Should().Be(erpType);
    }
}
