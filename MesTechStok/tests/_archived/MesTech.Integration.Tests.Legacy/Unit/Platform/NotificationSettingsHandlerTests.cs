using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// Notification, Settings, Finance handler null-guard testleri.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Settings")]
[Trait("Group", "Handler")]
public class NotificationSettingsHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();

    public NotificationSettingsHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ MarkNotificationRead ═══

    [Fact]
    public async Task MarkNotificationRead_NullRequest_Throws()
    {
        var repo = new Mock<INotificationLogRepository>();
        var handler = new MarkNotificationReadHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetNotificationSettings ═══

    [Fact]
    public async Task GetNotificationSettings_NullRequest_Throws()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var handler = new GetNotificationSettingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetGeneralSettings ═══

    [Fact]
    public async Task GetGeneralSettings_NullRequest_Throws()
    {
        var repo = new Mock<ITenantRepository>();
        var handler = new GetGeneralSettingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetProfileSettings ═══

    [Fact]
    public async Task GetProfileSettings_NullRequest_Throws()
    {
        var repo = new Mock<ITenantRepository>();
        var handler = new GetProfileSettingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ UpdateProfileSettings ═══

    [Fact]
    public async Task UpdateProfileSettings_NullRequest_Throws()
    {
        var repo = new Mock<ITenantRepository>();
        var handler = new UpdateProfileSettingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateExpense ═══

    [Fact]
    public async Task CreateExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFinanceExpenseRepository>();
        var handler = new CreateExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetCashRegisters ═══

    [Fact]
    public async Task GetCashRegisters_NullRequest_Throws()
    {
        var repo = new Mock<ICashRegisterRepository>();
        var handler = new GetCashRegistersHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetBankAccounts ═══

    [Fact]
    public async Task GetBankAccounts_NullRequest_Throws()
    {
        var repo = new Mock<IBankAccountRepository>();
        var handler = new GetBankAccountsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
