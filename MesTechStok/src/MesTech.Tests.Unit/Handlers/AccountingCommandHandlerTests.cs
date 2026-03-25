using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for accounting command handlers — ChartOfAccount CRUD, CloseAccountingPeriod,
/// CreateBaBsRecord, GenerateBaBsReport, CreateFinancialGoal.
/// </summary>
[Trait("Category", "Unit")]
public class AccountingCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ CreateChartOfAccountHandler ═══════

    [Fact]
    public async Task CreateChartOfAccount_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateChartOfAccountHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateChartOfAccount_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        var sut = new CreateChartOfAccountHandler(repo.Object, uow.Object);
        var cmd = new CreateChartOfAccountCommand(_tenantId, "100", "Kasa", AccountType.Asset);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChartOfAccount_DuplicateCode_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        var existing = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        repo.Setup(r => r.GetByCodeAsync(It.IsAny<Guid>(), "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new CreateChartOfAccountHandler(repo.Object, uow.Object);
        var cmd = new CreateChartOfAccountCommand(_tenantId, "100", "Kasa Tekrar", AccountType.Asset);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ DeleteChartOfAccountHandler ═══════

    [Fact]
    public async Task DeleteChartOfAccount_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new DeleteChartOfAccountHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteChartOfAccount_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        var sut = new DeleteChartOfAccountHandler(repo.Object, uow.Object);
        var cmd = new DeleteChartOfAccountCommand(Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ UpdateChartOfAccountHandler ═══════

    [Fact]
    public async Task UpdateChartOfAccount_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateChartOfAccountHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateChartOfAccount_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        var sut = new UpdateChartOfAccountHandler(repo.Object, uow.Object);
        var cmd = new UpdateChartOfAccountCommand(Guid.NewGuid(), "Yeni Isim");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ CloseAccountingPeriodHandler ═══════

    [Fact]
    public async Task CloseAccountingPeriod_NewPeriod_CreatesAndCloses()
    {
        var repo = new Mock<IAccountingPeriodRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<CloseAccountingPeriodHandler>>();
        repo.Setup(r => r.GetByYearMonthAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountingPeriod?)null);

        var sut = new CloseAccountingPeriodHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new CloseAccountingPeriodCommand(_tenantId, 2026, 3, "admin");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<AccountingPeriod>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateBaBsRecordHandler ═══════

    [Fact]
    public async Task CreateBaBsRecord_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IBaBsRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateBaBsRecordHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBaBsRecord_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IBaBsRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateBaBsRecordHandler(repo.Object, uow.Object);
        var cmd = new CreateBaBsRecordCommand(_tenantId, 2026, 3, BaBsType.Ba, "1234567890", "Tedarikci A.S.", 15000m, 5);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<BaBsRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ GenerateBaBsReportHandler ═══════

    [Fact]
    public async Task GenerateBaBsReport_NullRequest_ThrowsArgumentNullException()
    {
        var service = new Mock<IBaBsReportService>();
        var sut = new GenerateBaBsReportHandler(service.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateBaBsReport_ValidRequest_CallsService()
    {
        var service = new Mock<IBaBsReportService>();
        var expected = new BaBsReportDto();
        service.Setup(s => s.GenerateBaBsReportAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new GenerateBaBsReportHandler(service.Object);
        var query = new GenerateBaBsReportQuery(_tenantId, 2026, 3);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(s => s.GenerateBaBsReportAsync(_tenantId, 2026, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CreateFinancialGoalHandler ═══════

    [Fact]
    public async Task CreateFinancialGoal_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFinancialGoalRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFinancialGoalHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFinancialGoal_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IFinancialGoalRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFinancialGoalHandler(repo.Object, uow.Object);
        var cmd = new CreateFinancialGoalCommand(
            _tenantId, "Yillik Gelir Hedefi", 500_000m,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<FinancialGoal>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
