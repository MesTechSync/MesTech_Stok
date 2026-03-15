using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// AuditLog entity testleri.
/// Factory method, validation, ve property dogrulama senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AuditLog")]
[Trait("Phase", "Dalga12")]
public class AuditLogTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    // ══════════════════════════════════════════════════════════════════════════
    // Create Factory Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Create — valid parameters produce AuditLog with all fields set")]
    public void Create_ValidParameters_ReturnsAuditLogWithAllFields()
    {
        // Act
        var log = AuditLog.Create(
            _tenantId,
            _userId,
            "admin@mestech.com",
            "Update",
            "Product",
            Guid.NewGuid(),
            oldValues: """{"Price":100}""",
            newValues: """{"Price":150}""",
            ipAddress: "192.168.1.100");

        // Assert
        log.Id.Should().NotBe(Guid.Empty);
        log.TenantId.Should().Be(_tenantId);
        log.UserId.Should().Be(_userId);
        log.UserName.Should().Be("admin@mestech.com");
        log.Action.Should().Be("Update");
        log.EntityType.Should().Be("Product");
        log.EntityId.Should().NotBeNull();
        log.OldValues.Should().Contain("100");
        log.NewValues.Should().Contain("150");
        log.IpAddress.Should().Be("192.168.1.100");
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Create — optional fields can be null")]
    public void Create_OptionalFieldsNull_Succeeds()
    {
        var log = AuditLog.Create(
            _tenantId,
            userId: null,
            userName: "system",
            action: "Delete",
            entityType: "Order",
            entityId: null,
            oldValues: null,
            newValues: null,
            ipAddress: null);

        log.UserId.Should().BeNull();
        log.EntityId.Should().BeNull();
        log.OldValues.Should().BeNull();
        log.NewValues.Should().BeNull();
        log.IpAddress.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Validation Tests — Required Fields
    // ══════════════════════════════════════════════════════════════════════════

    [Theory(DisplayName = "Create — empty/null userName throws ArgumentException")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyUserName_ThrowsArgumentException(string? userName)
    {
        var act = () => AuditLog.Create(
            _tenantId, _userId, userName!, "Update", "Product", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Create — empty/null action throws ArgumentException")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyAction_ThrowsArgumentException(string? action)
    {
        var act = () => AuditLog.Create(
            _tenantId, _userId, "user@test.com", action!, "Product", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Theory(DisplayName = "Create — empty/null entityType throws ArgumentException")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyEntityType_ThrowsArgumentException(string? entityType)
    {
        var act = () => AuditLog.Create(
            _tenantId, _userId, "user@test.com", "Update", entityType!, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ToString
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "ToString — contains timestamp, userName, action, entityType")]
    public void ToString_ContainsRelevantInfo()
    {
        var entityId = Guid.NewGuid();
        var log = AuditLog.Create(
            _tenantId, _userId, "ahmet@kozmetik.com", "Create", "Invoice", entityId);

        var str = log.ToString();

        str.Should().Contain("ahmet@kozmetik.com");
        str.Should().Contain("Create");
        str.Should().Contain("Invoice");
        str.Should().Contain(entityId.ToString());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Unique ID Generation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Create — each log gets a unique ID")]
    public void Create_EachLogGetsUniqueId()
    {
        var log1 = AuditLog.Create(_tenantId, _userId, "user1", "Create", "Product", Guid.NewGuid());
        var log2 = AuditLog.Create(_tenantId, _userId, "user2", "Update", "Product", Guid.NewGuid());

        log1.Id.Should().NotBe(log2.Id);
    }
}
