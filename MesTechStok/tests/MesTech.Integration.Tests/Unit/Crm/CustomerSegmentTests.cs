using FluentAssertions;
using MesTech.Domain.Entities;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Crm;

/// <summary>
/// EMR-09 ALAN-F — Customer entity segment testleri.
/// IsVip ve varsayilan deger dogrulamalari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Crm")]
[Trait("Group", "CustomerSegment")]
public class CustomerSegmentTests
{
    // ═══════════════════════════════════════════════════════════════════
    // 1. VIP musteri — IsVip true
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void VipCustomer_IsVipTrue()
    {
        // Arrange & Act
        var customer = new Customer
        {
            TenantId = Guid.NewGuid(),
            Code = "VIP-001",
            Name = "Global Import A.S.",
            Segment = "Enterprise"
        };
        customer.PromoteToVip();

        // Assert
        customer.IsVip.Should().BeTrue();
        customer.Segment.Should().Be("Enterprise");
        customer.IsActive.Should().BeTrue("varsayilan olarak aktif olmali");
        customer.DisplayName.Should().Be("Global Import A.S.");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. Varsayilan musteri — IsVip false
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultCustomer_IsVipFalse()
    {
        // Arrange & Act
        var customer = new Customer
        {
            TenantId = Guid.NewGuid(),
            Code = "MUS-001",
            Name = "Kucuk Isletme Ltd."
        };

        // Assert
        customer.IsVip.Should().BeFalse("yeni musteri varsayilan olarak VIP degildir");
        customer.IsActive.Should().BeTrue("yeni musteri varsayilan olarak aktif olmali");
        customer.IsBlocked.Should().BeFalse("yeni musteri bloklanmamis olmali");
        customer.CurrentBalance.Should().Be(0m, "yeni musteri bakiyesi sifir olmali");
        customer.CustomerType.Should().Be("INDIVIDUAL", "varsayilan musteri tipi INDIVIDUAL");
        customer.Currency.Should().Be("TRY", "varsayilan para birimi TRY");
    }
}
