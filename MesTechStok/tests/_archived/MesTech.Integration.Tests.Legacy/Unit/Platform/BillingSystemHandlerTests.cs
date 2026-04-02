using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Features.Billing.Commands.ProcessPaymentWebhook;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
using MesTech.Application.Features.Billing.Queries.GetUserFeatures;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.Documents.Queries.GetDocuments;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Billing")]
[Trait("Group", "Handler")]
public class BillingSystemHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public BillingSystemHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ BILLING ═══
    [Fact] public async Task CancelSubscription_Null_Throws() { var r = new Mock<ITenantSubscriptionRepository>(); var h = new CancelSubscriptionHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateBillingInvoice_Null_Throws() { var r = new Mock<IBillingInvoiceRepository>(); var h = new CreateBillingInvoiceHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateSubscription_Null_Throws() { var r = new Mock<ITenantSubscriptionRepository>(); var p = new Mock<ISubscriptionPlanRepository>(); var h = new CreateSubscriptionHandler(r.Object, p.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ChangeSubscriptionPlan_Null_Throws() { var r = new Mock<ITenantSubscriptionRepository>(); var p = new Mock<ISubscriptionPlanRepository>(); var h = new ChangeSubscriptionPlanHandler(r.Object, p.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBillingInvoices_Null_Throws() { var r = new Mock<IBillingInvoiceRepository>(); var h = new GetBillingInvoicesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetSubscriptionPlans_Null_Throws() { var r = new Mock<ISubscriptionPlanRepository>(); var h = new GetSubscriptionPlansHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetTenantSubscription_Null_Throws() { var r = new Mock<ITenantSubscriptionRepository>(); var h = new GetTenantSubscriptionHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetUserFeatures_Null_Throws() { var r = new Mock<ITenantSubscriptionRepository>(); var h = new GetUserFeaturesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ SYSTEM ═══
    [Fact] public async Task MarkAllUserNotificationsRead_Null_Throws() { var r = new Mock<IUserNotificationRepository>(); var h = new MarkAllUserNotificationsReadHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task MarkUserNotificationRead_Null_Throws() { var r = new Mock<IUserNotificationRepository>(); var h = new MarkUserNotificationReadHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetUnreadNotificationCount_Null_Throws() { var r = new Mock<IUserNotificationRepository>(); var h = new GetUnreadNotificationCountHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetUserNotifications_Null_Throws() { var r = new Mock<IUserNotificationRepository>(); var h = new GetUserNotificationsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetLaunchReadiness_Null_Throws() { var pr = new Mock<IProductRepository>(); var or2 = new Mock<IOrderRepository>(); var h = new GetLaunchReadinessHandler(pr.Object, or2.Object, Mock.Of<ILogger<GetLaunchReadinessHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetAuditLogs_Null_Throws() { var h = new GetAuditLogsHandler(); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBackupHistory_Null_Throws() { var h = new GetBackupHistoryHandler(Mock.Of<ILogger<GetBackupHistoryHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetKvkkAuditLogs_Null_Throws() { var r = new Mock<IKvkkAuditLogRepository>(); var h = new GetKvkkAuditLogsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDocuments_Null_Throws() { var r = new Mock<IDocumentRepository>(); var h = new GetDocumentsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
