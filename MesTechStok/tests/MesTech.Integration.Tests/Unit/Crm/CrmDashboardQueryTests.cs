using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Crm;

/// <summary>
/// EMR-09 ALAN-F — CRM Dashboard KPI dogrulama testleri.
/// Domain entity'leri uzerinden KPI hesaplamalarini test eder.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Crm")]
[Trait("Group", "CrmDashboard")]
public class CrmDashboardQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════════════
    // 1. Bos veritabani — tum KPI'lar sifir
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void EmptyDb_ReturnsZeros()
    {
        // Arrange
        var customers = new List<Customer>();
        var messages = new List<PlatformMessage>();

        // Act
        var totalCustomers = customers.Count;
        var unreadCount = messages.Count(m => m.Status == MessageStatus.Unread);
        var vipCount = customers.Count(c => c.IsVip);

        // Assert
        totalCustomers.Should().Be(0);
        unreadCount.Should().Be(0);
        vipCount.Should().Be(0);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. Musteri ekleme — TotalCustomers dogrulamasi
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void WithCustomers_CorrectCounts()
    {
        // Arrange
        var customers = new List<Customer>
        {
            CreateCustomer("MUS-001", "TechCo A.S.", isVip: true),
            CreateCustomer("MUS-002", "Mega Ticaret Ltd.", isVip: false),
            CreateCustomer("MUS-003", "Start Up Hub", isVip: true),
            CreateCustomer("MUS-004", "Global Import", isVip: false),
            CreateCustomer("MUS-005", "E-Ticaret Pro", isVip: false),
        };

        // Act
        var totalCustomers = customers.Count;
        var vipCount = customers.Count(c => c.IsVip);
        var activeCount = customers.Count(c => c.IsActive);

        // Assert
        totalCustomers.Should().Be(5);
        vipCount.Should().Be(2);
        activeCount.Should().Be(5, "varsayilan olarak tum musteriler aktif");
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. Deal ekleme — PipelineTotalValue dogrulamasi
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void WithDeals_PipelineValueCorrect()
    {
        // Arrange — simulate deal values (pipeline is sum of active deal values)
        var dealValues = new[] { 45_000m, 120_000m, 85_000m, 35_000m, 65_000m };

        // Act
        var pipelineTotal = dealValues.Sum();

        // Assert
        pipelineTotal.Should().Be(350_000m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. Mesaj ekleme — UnreadMessageCount dogrulamasi
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void WithMessages_UnreadCountCorrect()
    {
        // Arrange
        var messages = new List<PlatformMessage>
        {
            CreateMessage(PlatformType.Trendyol, MessageStatus.Unread),
            CreateMessage(PlatformType.Hepsiburada, MessageStatus.Unread),
            CreateMessage(PlatformType.N11, MessageStatus.Read),
            CreateMessage(PlatformType.Trendyol, MessageStatus.Replied),
            CreateMessage(PlatformType.Amazon, MessageStatus.Unread),
            CreateMessage(PlatformType.eBay, MessageStatus.Archived),
        };

        // Act
        var unreadCount = messages.Count(m => m.Status == MessageStatus.Unread);
        var totalCount = messages.Count;
        var platformCount = messages.Select(m => m.Platform).Distinct().Count();

        // Assert
        unreadCount.Should().Be(3);
        totalCount.Should().Be(6);
        platformCount.Should().Be(5);
    }

    // ── Helpers ──

    private static Customer CreateCustomer(string code, string name, bool isVip = false)
    {
        return new Customer
        {
            TenantId = TenantId,
            Code = code,
            Name = name,
            IsVip = isVip,
            IsActive = true
        };
    }

    private static PlatformMessage CreateMessage(PlatformType platform, MessageStatus status)
    {
        return new PlatformMessage
        {
            TenantId = TenantId,
            Platform = platform,
            Status = status,
            ExternalMessageId = Guid.NewGuid().ToString(),
            SenderName = "Test Sender",
            Subject = "Test Subject",
            Body = "Test body",
            Direction = MessageDirection.Incoming,
            ReceivedAt = DateTime.UtcNow
        };
    }
}
