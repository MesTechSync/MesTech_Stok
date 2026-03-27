using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Hr;
using MesTech.Domain.Events.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for miscellaneous event handlers — invoice, lead, leave, notification,
/// onboarding, profit, return, subscription, task, and tax withholding handlers.
/// </summary>
[Trait("Category", "Unit")]
public class MiscEventHandlerTests
{
    #region InvoiceApprovedEventHandler

    [Fact]
    public async Task InvoiceApprovedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new InvoiceApprovedEventHandler(
            NullLogger<InvoiceApprovedEventHandler>.Instance);

        var evt = new InvoiceApprovedEvent(
            InvoiceId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            InvoiceNumber: "INV-2026-001",
            GrandTotal: 5000m,
            TaxAmount: 900m,
            NetAmount: 4100m,
            Type: InvoiceType.EFatura,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(InvoiceType.EFatura)]
    [InlineData(InvoiceType.EArsiv)]
    [InlineData(InvoiceType.EIrsaliye)]
    public async Task InvoiceApprovedEventHandler_VariousTypes_CompletesSuccessfully(InvoiceType type)
    {
        var sut = new InvoiceApprovedEventHandler(
            NullLogger<InvoiceApprovedEventHandler>.Instance);

        var evt = new InvoiceApprovedEvent(
            InvoiceId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            InvoiceNumber: $"INV-{type}",
            GrandTotal: 1000m,
            TaxAmount: 180m,
            NetAmount: 820m,
            Type: type,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region InvoiceGeneratedForERPEventHandler

    [Fact]
    public async Task InvoiceGeneratedForERPEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new InvoiceGeneratedForERPEventHandler(
            NullLogger<InvoiceGeneratedForERPEventHandler>.Instance);

        var evt = new InvoiceGeneratedForERPEvent(
            InvoiceId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            InvoiceNumber: "INV-ERP-001",
            TotalAmount: 7500m,
            TargetERP: "Parasut",
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region LeadConvertedEventHandler

    [Fact]
    public async Task LeadConvertedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new LeadConvertedEventHandler(
            NullLogger<LeadConvertedEventHandler>.Instance);

        var evt = new LeadConvertedEvent(
            LeadId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            CrmContactId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region LeaveApprovedEventHandler

    [Fact]
    public async Task LeaveApprovedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new LeaveApprovedEventHandler(
            NullLogger<LeaveApprovedEventHandler>.Instance);

        var evt = new LeaveApprovedEvent(
            LeaveId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            EmployeeId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region LeaveRejectedEventHandler

    [Fact]
    public async Task LeaveRejectedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new LeaveRejectedEventHandler(
            NullLogger<LeaveRejectedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            leaveId: Guid.NewGuid(),
            employeeId: Guid.NewGuid(),
            reason: "Insufficient leave balance",
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region NotificationSettingsUpdatedEventHandler

    [Fact]
    public async Task NotificationSettingsUpdatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new NotificationSettingsUpdatedEventHandler(
            NullLogger<NotificationSettingsUpdatedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            userId: Guid.NewGuid(),
            channel: NotificationChannel.Telegram,
            isEnabled: true,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(NotificationChannel.Email, true)]
    [InlineData(NotificationChannel.Telegram, false)]
    [InlineData(NotificationChannel.WhatsApp, true)]
    [InlineData(NotificationChannel.Push, false)]
    [InlineData(NotificationChannel.SMS, true)]
    public async Task NotificationSettingsUpdatedEventHandler_AllChannels_CompletesSuccessfully(
        NotificationChannel channel, bool isEnabled)
    {
        var sut = new NotificationSettingsUpdatedEventHandler(
            NullLogger<NotificationSettingsUpdatedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            userId: Guid.NewGuid(),
            channel: channel,
            isEnabled: isEnabled,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region OnboardingCompletedEventHandler

    [Fact]
    public async Task OnboardingCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new OnboardingCompletedEventHandler(
            NullLogger<OnboardingCompletedEventHandler>.Instance);

        var started = DateTime.UtcNow.AddDays(-3);
        var completed = DateTime.UtcNow;

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            onboardingProgressId: Guid.NewGuid(),
            startedAt: started,
            completedAt: completed,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ProfitReportGeneratedEventHandler

    [Fact]
    public async Task ProfitReportGeneratedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ProfitReportGeneratedEventHandler(
            NullLogger<ProfitReportGeneratedEventHandler>.Instance);

        var evt = new ProfitReportGeneratedEvent
        {
            ReportId = Guid.NewGuid(),
            Period = "2026-03",
            Platform = "Trendyol",
            NetProfit = 15000m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProfitReportGeneratedEventHandler_NullPlatform_CompletesSuccessfully()
    {
        var sut = new ProfitReportGeneratedEventHandler(
            NullLogger<ProfitReportGeneratedEventHandler>.Instance);

        var evt = new ProfitReportGeneratedEvent
        {
            ReportId = Guid.NewGuid(),
            Period = "2026-Q1",
            Platform = null,
            NetProfit = -500m
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ReturnCreatedEventHandler

    [Fact]
    public async Task ReturnCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ReturnCreatedEventHandler(
            NullLogger<ReturnCreatedEventHandler>.Instance);

        var evt = new ReturnCreatedEvent(
            ReturnRequestId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            Platform: PlatformType.Trendyol,
            Reason: ReturnReason.DefectiveProduct,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(ReturnReason.WrongProduct)]
    [InlineData(ReturnReason.DamagedInShipping)]
    [InlineData(ReturnReason.CustomerRegret)]
    public async Task ReturnCreatedEventHandler_VariousReasons_CompletesSuccessfully(ReturnReason reason)
    {
        var sut = new ReturnCreatedEventHandler(
            NullLogger<ReturnCreatedEventHandler>.Instance);

        var evt = new ReturnCreatedEvent(
            ReturnRequestId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            Platform: PlatformType.OpenCart,
            Reason: reason,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ReturnResolvedEventHandler

    [Fact]
    public async Task ReturnResolvedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new ReturnResolvedEventHandler(
            NullLogger<ReturnResolvedEventHandler>.Instance);

        var evt = new ReturnResolvedEvent(
            ReturnRequestId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            FinalStatus: ReturnStatus.Refunded,
            RefundAmount: 299.99m,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(ReturnStatus.Approved)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Cancelled)]
    public async Task ReturnResolvedEventHandler_VariousStatuses_CompletesSuccessfully(ReturnStatus status)
    {
        var sut = new ReturnResolvedEventHandler(
            NullLogger<ReturnResolvedEventHandler>.Instance);

        var evt = new ReturnResolvedEvent(
            ReturnRequestId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            FinalStatus: status,
            RefundAmount: 0m,
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SubscriptionCancelledEventHandler

    [Fact]
    public async Task SubscriptionCancelledEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new SubscriptionCancelledEventHandler(
            NullLogger<SubscriptionCancelledEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            reason: "Switching to competitor",
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubscriptionCancelledEventHandler_NullReason_CompletesSuccessfully()
    {
        var sut = new SubscriptionCancelledEventHandler(
            NullLogger<SubscriptionCancelledEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            reason: null,
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SubscriptionCreatedEventHandler

    [Fact]
    public async Task SubscriptionCreatedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new SubscriptionCreatedEventHandler(
            NullLogger<SubscriptionCreatedEventHandler>.Instance);

        var act = () => sut.HandleAsync(
            tenantId: Guid.NewGuid(),
            subscriptionId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            ct: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region TaskCompletedEventHandler

    [Fact]
    public async Task TaskCompletedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new TaskCompletedEventHandler(
            NullLogger<TaskCompletedEventHandler>.Instance);

        var evt = new TaskCompletedEvent(
            TaskId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            CompletedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region TaskOverdueEventHandler

    [Fact]
    public async Task TaskOverdueEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new TaskOverdueEventHandler(
            NullLogger<TaskOverdueEventHandler>.Instance);

        var evt = new TaskOverdueEvent(
            TaskId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            DueDate: DateTime.UtcNow.AddDays(-2),
            OccurredAt: DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region TaxWithholdingComputedEventHandler

    [Fact]
    public async Task TaxWithholdingComputedEventHandler_HandleAsync_CompletesSuccessfully()
    {
        var sut = new TaxWithholdingComputedEventHandler(
            NullLogger<TaxWithholdingComputedEventHandler>.Instance);

        var evt = new TaxWithholdingComputedEvent
        {
            TaxWithholdingId = Guid.NewGuid(),
            TaxExclusiveAmount = 10000m,
            Rate = 0.20m,
            WithholdingAmount = 2000m,
            TaxType = "GelirVergisi"
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TaxWithholdingComputedEventHandler_ZeroRate_CompletesSuccessfully()
    {
        var sut = new TaxWithholdingComputedEventHandler(
            NullLogger<TaxWithholdingComputedEventHandler>.Instance);

        var evt = new TaxWithholdingComputedEvent
        {
            TaxWithholdingId = Guid.NewGuid(),
            TaxExclusiveAmount = 5000m,
            Rate = 0m,
            WithholdingAmount = 0m,
            TaxType = "KDV"
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    #endregion
}
