using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// UserNotification entity testleri.
/// Factory method, MarkAsRead state transition, ve category atama kontrolu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
[Trait("Phase", "I-11")]
public class UserNotificationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact(DisplayName = "MarkAsRead — sets IsRead and ReadAt timestamp")]
    public void UserNotification_MarkAsRead_ShouldSetTimestamp()
    {
        // Arrange
        var notification = UserNotification.Create(
            _tenantId, _userId, "Test Title", "Test Message", NotificationCategory.Order);

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Create — factory sets correct defaults (IsRead=false, ReadAt=null)")]
    public void UserNotification_Create_ShouldSetDefaults()
    {
        // Act
        var notification = UserNotification.Create(
            _tenantId, _userId, "New Order", "Order #1234 received", NotificationCategory.Order);

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.Title.Should().Be("New Order");
        notification.Message.Should().Be("Order #1234 received");
        notification.UserId.Should().Be(_userId);
        notification.TenantId.Should().Be(_tenantId);
    }

    [Fact(DisplayName = "Create — category should be correctly assigned")]
    public void UserNotification_Category_ShouldBeAssigned()
    {
        // Act
        var notification = UserNotification.Create(
            _tenantId, _userId, "Low Stock", "Product X below threshold", NotificationCategory.Stock);

        // Assert
        notification.Category.Should().Be(NotificationCategory.Stock);
    }
}
