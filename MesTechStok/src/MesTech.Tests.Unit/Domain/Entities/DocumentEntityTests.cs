using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Document, Log, Notification entity domain behavior tests.
/// Document, DocumentFolder, SavedReport, LogEntry, AuditLog, AccessLog,
/// LoginAttempt, NotificationLog, NotificationSetting, NotificationTemplate,
/// UserNotification, WebhookLog, ApiCallLog.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DocumentEntities")]
[Trait("Phase", "Dalga15")]
public class DocumentEntityTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    // ═══════════════════════════════════════════
    // Document
    // ═══════════════════════════════════════════

    [Fact]
    public void Document_Create_SetsFieldsAndRaisesEvent()
    {
        var doc = Document.Create(TenantId, "file.pdf", "original.pdf",
            "application/pdf", 1024, "/storage/file.pdf", UserId);

        doc.FileName.Should().Be("file.pdf");
        doc.OriginalFileName.Should().Be("original.pdf");
        doc.FileSizeBytes.Should().Be(1024);
        doc.StoragePath.Should().Be("/storage/file.pdf");
        doc.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DocumentUploadedEvent");
    }

    [Fact]
    public void Document_Create_EmptyFileName_Throws()
    {
        var act = () => Document.Create(TenantId, "", "orig.pdf", "application/pdf",
            1024, "/path", UserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Document_Create_ZeroFileSize_Throws()
    {
        var act = () => Document.Create(TenantId, "f.pdf", "f.pdf", "application/pdf",
            0, "/path", UserId);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Document_Create_EmptyStoragePath_Throws()
    {
        var act = () => Document.Create(TenantId, "f.pdf", "f.pdf", "application/pdf",
            1024, "", UserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Document_LinkToOrder_SetsOrderId()
    {
        var doc = Document.Create(TenantId, "f.pdf", "f.pdf", "application/pdf",
            1024, "/path", UserId);
        var orderId = Guid.NewGuid();

        doc.LinkToOrder(orderId);

        doc.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Document_MoveToFolder_SetsFolderId()
    {
        var doc = Document.Create(TenantId, "f.pdf", "f.pdf", "application/pdf",
            1024, "/path", UserId);
        var folderId = Guid.NewGuid();

        doc.MoveToFolder(folderId);

        doc.FolderId.Should().Be(folderId);
    }

    // ═══════════════════════════════════════════
    // DocumentFolder
    // ═══════════════════════════════════════════

    [Fact]
    public void DocumentFolder_Create_SetsNameAndTenant()
    {
        var folder = DocumentFolder.Create(TenantId, "Invoices");

        folder.Name.Should().Be("Invoices");
        folder.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void DocumentFolder_Create_EmptyName_Throws()
    {
        var act = () => DocumentFolder.Create(TenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DocumentFolder_Rename_UpdatesName()
    {
        var folder = DocumentFolder.Create(TenantId, "Old Name");

        folder.Rename("New Name");

        folder.Name.Should().Be("New Name");
    }

    [Fact]
    public void DocumentFolder_Rename_SystemFolder_Throws()
    {
        var folder = DocumentFolder.Create(TenantId, "System", isSystem: true);

        var act = () => folder.Rename("New Name");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DocumentFolder_Delete_SystemFolder_Throws()
    {
        var folder = DocumentFolder.Create(TenantId, "System", isSystem: true);

        var act = () => folder.Delete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DocumentFolder_Delete_NormalFolder_SoftDeletes()
    {
        var folder = DocumentFolder.Create(TenantId, "Temp");

        folder.Delete();

        folder.IsDeleted.Should().BeTrue();
        folder.DeletedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════
    // SavedReport
    // ═══════════════════════════════════════════

    [Fact]
    public void SavedReport_Create_SetsFieldsAndDefaultFalse()
    {
        var report = SavedReport.Create(TenantId, "Monthly Sales", "Sales",
            "{\"from\":\"2026-01\"}", UserId);

        report.Name.Should().Be("Monthly Sales");
        report.ReportType.Should().Be("Sales");
        report.IsDefault.Should().BeFalse();
        report.LastExecutedAt.Should().BeNull();
    }

    [Fact]
    public void SavedReport_Create_EmptyName_Throws()
    {
        var act = () => SavedReport.Create(TenantId, "", "Sales", "{}", UserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SavedReport_MarkExecuted_SetsTimestamp()
    {
        var report = SavedReport.Create(TenantId, "Report", "Sales", "{}", UserId);

        report.MarkExecuted();

        report.LastExecutedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════
    // LogEntry (POCO)
    // ═══════════════════════════════════════════

    [Fact]
    public void LogEntry_CreationWithDefaults()
    {
        var log = new LogEntry { TenantId = TenantId, Message = "Test log" };

        log.Level.Should().Be("Info");
        log.Category.Should().Be("General");
        log.Message.Should().Be("Test log");
    }

    // ═══════════════════════════════════════════
    // AuditLog
    // ═══════════════════════════════════════════

    [Fact]
    public void AuditLog_Create_SetsAllFields()
    {
        var entityId = Guid.NewGuid();
        var log = AuditLog.Create(TenantId, UserId, "admin", "Update",
            "Product", entityId, "{\"old\":1}", "{\"new\":2}", "127.0.0.1");

        log.UserName.Should().Be("admin");
        log.Action.Should().Be("Update");
        log.EntityType.Should().Be("Product");
        log.EntityId.Should().Be(entityId);
        log.OldValues.Should().Be("{\"old\":1}");
        log.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public void AuditLog_Create_EmptyUserName_Throws()
    {
        var act = () => AuditLog.Create(TenantId, UserId, "", "Update", "Product", null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditLog_Create_EmptyAction_Throws()
    {
        var act = () => AuditLog.Create(TenantId, UserId, "admin", "", "Product", null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuditLog_Create_EmptyEntityType_Throws()
    {
        var act = () => AuditLog.Create(TenantId, UserId, "admin", "Update", "", null);
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // AccessLog (POCO)
    // ═══════════════════════════════════════════

    [Fact]
    public void AccessLog_CreationWithDefaults()
    {
        var log = new AccessLog
        {
            TenantId = TenantId,
            UserId = UserId,
            Action = "Read",
            Resource = "/api/products",
            IsAllowed = true
        };

        log.Action.Should().Be("Read");
        log.IsAllowed.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // LoginAttempt
    // ═══════════════════════════════════════════

    [Fact]
    public void LoginAttempt_Create_SetsFields()
    {
        var attempt = LoginAttempt.Create("admin", "192.168.1.1", true, "Chrome");

        attempt.Username.Should().Be("admin");
        attempt.IpAddress.Should().Be("192.168.1.1");
        attempt.Success.Should().BeTrue();
        attempt.UserAgent.Should().Be("Chrome");
    }

    [Fact]
    public void LoginAttempt_Create_FailedAttempt()
    {
        var attempt = LoginAttempt.Create("hacker", "10.0.0.1", false);

        attempt.Success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // NotificationLog
    // ═══════════════════════════════════════════

    [Fact]
    public void NotificationLog_Create_StartsPending()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "LowStock", "Stock is low");

        log.Status.Should().Be(NotificationStatus.Pending);
        log.Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public void NotificationLog_Create_EmptyRecipient_Throws()
    {
        var act = () => NotificationLog.Create(TenantId, NotificationChannel.Email,
            "", "Template", "Content");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotificationLog_MarkAsSent_FromPending_Succeeds()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");

        log.MarkAsSent();

        log.Status.Should().Be(NotificationStatus.Sent);
        log.SentAt.Should().NotBeNull();
    }

    [Fact]
    public void NotificationLog_MarkAsSent_FromSent_Throws()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");
        log.MarkAsSent();

        var act = () => log.MarkAsSent();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NotificationLog_MarkAsDelivered_FromSent_Succeeds()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");
        log.MarkAsSent();

        log.MarkAsDelivered();

        log.Status.Should().Be(NotificationStatus.Delivered);
    }

    [Fact]
    public void NotificationLog_MarkAsDelivered_FromPending_Throws()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");

        var act = () => log.MarkAsDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NotificationLog_MarkAsFailed_FromPending_Succeeds()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");

        log.MarkAsFailed("SMTP error");

        log.Status.Should().Be(NotificationStatus.Failed);
        log.ErrorMessage.Should().Be("SMTP error");
    }

    [Fact]
    public void NotificationLog_MarkAsFailed_EmptyReason_Throws()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");

        var act = () => log.MarkAsFailed("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotificationLog_MarkAsRead_FromDelivered_Succeeds()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");
        log.MarkAsSent();
        log.MarkAsDelivered();

        log.MarkAsRead();

        log.Status.Should().Be(NotificationStatus.Read);
        log.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void NotificationLog_FullLifecycle_PendingToRead()
    {
        var log = NotificationLog.Create(TenantId, NotificationChannel.Email,
            "user@test.com", "Template", "Content");

        log.MarkAsSent();
        log.MarkAsDelivered();
        log.MarkAsRead();

        log.Status.Should().Be(NotificationStatus.Read);
    }

    // ═══════════════════════════════════════════
    // NotificationSetting
    // ═══════════════════════════════════════════

    [Fact]
    public void NotificationSetting_ShouldNotify_DisabledChannel_ReturnsFalse()
    {
        var setting = new NotificationSetting
        {
            TenantId = TenantId, UserId = UserId,
            Channel = NotificationChannel.Email, IsEnabled = false
        };

        setting.ShouldNotify(NotificationCategory.Order).Should().BeFalse();
    }

    [Fact]
    public void NotificationSetting_ShouldNotify_OrderCategory_ReturnsTrue()
    {
        var setting = new NotificationSetting
        {
            TenantId = TenantId, UserId = UserId,
            Channel = NotificationChannel.Email, IsEnabled = true,
            NotifyOnOrderReceived = true
        };

        setting.ShouldNotify(NotificationCategory.Order).Should().BeTrue();
    }

    [Fact]
    public void NotificationSetting_ShouldNotify_DuringQuietHours_ReturnsFalse()
    {
        var setting = new NotificationSetting
        {
            TenantId = TenantId, UserId = UserId,
            Channel = NotificationChannel.Email, IsEnabled = true,
            NotifyOnOrderReceived = true,
            QuietHoursStart = new TimeOnly(22, 0),
            QuietHoursEnd = new TimeOnly(8, 0)
        };

        // Test during quiet hours (e.g., 23:00)
        var midnight = new DateTime(2026, 1, 1, 23, 0, 0, DateTimeKind.Utc);
        setting.ShouldNotify(NotificationCategory.Order, midnight).Should().BeFalse();
    }

    [Fact]
    public void NotificationSetting_RequiresChannelAddress_Email_ReturnsTrue()
    {
        var setting = new NotificationSetting { Channel = NotificationChannel.Email };

        setting.RequiresChannelAddress().Should().BeTrue();
    }

    [Fact]
    public void NotificationSetting_RequiresChannelAddress_InApp_ReturnsFalse()
    {
        var setting = new NotificationSetting { Channel = NotificationChannel.Push };

        setting.RequiresChannelAddress().Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // NotificationTemplate
    // ═══════════════════════════════════════════

    [Fact]
    public void NotificationTemplate_Create_SetsActiveByDefault()
    {
        var template = NotificationTemplate.Create(TenantId, "LowStock",
            "Stock Alert", "{{productName}} is low", NotificationChannel.Email);

        template.IsActive.Should().BeTrue();
        template.Language.Should().Be("tr");
    }

    [Fact]
    public void NotificationTemplate_Create_EmptyTemplateName_Throws()
    {
        var act = () => NotificationTemplate.Create(TenantId, "", "Subject", "Body",
            NotificationChannel.Email);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotificationTemplate_Deactivate_SetsInactive()
    {
        var template = NotificationTemplate.Create(TenantId, "Test", "Subject",
            "Body", NotificationChannel.Email);

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void NotificationTemplate_Activate_SetsActive()
    {
        var template = NotificationTemplate.Create(TenantId, "Test", "Subject",
            "Body", NotificationChannel.Email);
        template.Deactivate();

        template.Activate();

        template.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // UserNotification
    // ═══════════════════════════════════════════

    [Fact]
    public void UserNotification_Create_StartsUnread()
    {
        var notification = UserNotification.Create(TenantId, UserId,
            "Low Stock", "Product X is low", NotificationCategory.Stock);

        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.Title.Should().Be("Low Stock");
    }

    [Fact]
    public void UserNotification_Create_EmptyTitle_Throws()
    {
        var act = () => UserNotification.Create(TenantId, UserId, "", "msg", NotificationCategory.Stock);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UserNotification_MarkAsRead_SetsReadAndTimestamp()
    {
        var notification = UserNotification.Create(TenantId, UserId,
            "Alert", "Message", NotificationCategory.System);

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void UserNotification_MarkAsRead_AlreadyRead_IsIdempotent()
    {
        var notification = UserNotification.Create(TenantId, UserId,
            "Alert", "Message", NotificationCategory.System);
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        notification.MarkAsRead();

        notification.ReadAt.Should().Be(firstReadAt);
    }

    // ═══════════════════════════════════════════
    // WebhookLog
    // ═══════════════════════════════════════════

    [Fact]
    public void WebhookLog_Create_ValidWebhook_SetsProcessedAt()
    {
        var log = WebhookLog.Create(TenantId, "Trendyol", "OrderCreated",
            "{\"orderId\":123}", "sig123", true);

        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.RetryCount.Should().Be(0);
    }

    [Fact]
    public void WebhookLog_Create_InvalidWebhook_SetsError()
    {
        var log = WebhookLog.Create(TenantId, "Trendyol", "OrderCreated",
            "{}", null, false, "Invalid signature");

        log.IsValid.Should().BeFalse();
        log.ProcessedAt.Should().BeNull();
        log.Error.Should().Be("Invalid signature");
    }

    [Fact]
    public void WebhookLog_MarkProcessed_SetsValidAndProcessedAt()
    {
        var log = WebhookLog.Create(TenantId, "Trendyol", "OrderCreated",
            "{}", null, false, "Error");

        log.MarkProcessed();

        log.IsValid.Should().BeTrue();
        log.ProcessedAt.Should().NotBeNull();
        log.Error.Should().BeNull();
    }

    [Fact]
    public void WebhookLog_IncrementRetry_IncreasesCountAndSetsError()
    {
        var log = WebhookLog.Create(TenantId, "Trendyol", "OrderCreated",
            "{}", null, false, "Error 1");

        log.IncrementRetry("Error 2");

        log.RetryCount.Should().Be(1);
        log.Error.Should().Be("Error 2");
    }

    // ═══════════════════════════════════════════
    // ApiCallLog (POCO)
    // ═══════════════════════════════════════════

    [Fact]
    public void ApiCallLog_CreationWithDefaults()
    {
        var log = new ApiCallLog
        {
            TenantId = TenantId,
            Endpoint = "/api/products",
            Method = "GET",
            Success = true,
            StatusCode = 200,
            DurationMs = 42
        };

        log.Endpoint.Should().Be("/api/products");
        log.Success.Should().BeTrue();
        log.DurationMs.Should().Be(42);
    }
}
