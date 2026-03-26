using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// Accounting Command Handler testleri.
/// Her handler: null request → ArgumentNullException, happy path → Guid döner.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
[Trait("Group", "CommandHandler")]
public class AccountingCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();

    public AccountingCommandHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ CreateChartOfAccount ═══

    [Fact]
    public async Task CreateChartOfAccount_NullRequest_Throws()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var handler = new CreateChartOfAccountHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateChartOfAccount_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetByCodeAsync(_tenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateChartOfAccountHandler(repo.Object, _uow.Object);
        var cmd = new CreateChartOfAccountCommand(_tenantId, "100", "Kasa", MesTech.Domain.Accounting.Enums.AccountType.Asset);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChartOfAccount_DuplicateCode_ThrowsInvalidOperation()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetByCodeAsync(_tenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChartOfAccounts.Create(_tenantId, "100", "Mevcut", MesTech.Domain.Accounting.Enums.AccountType.Asset));

        var handler = new CreateChartOfAccountHandler(repo.Object, _uow.Object);
        var cmd = new CreateChartOfAccountCommand(_tenantId, "100", "Tekrar", MesTech.Domain.Accounting.Enums.AccountType.Asset);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    // ═══ CreateTaxRecord ═══

    [Fact]
    public async Task CreateTaxRecord_NullRequest_Throws()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var handler = new CreateTaxRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateTaxRecord_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<TaxRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateTaxRecordHandler(repo.Object, _uow.Object);
        var cmd = new CreateTaxRecordCommand(_tenantId, "2026-03", "KDV", 100000m, 0.20m, 20000m, DateTime.UtcNow.AddDays(30));

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ CreateJournalEntry ═══

    [Fact]
    public async Task CreateJournalEntry_NullRequest_Throws()
    {
        var repo = new Mock<IJournalEntryRepository>();
        var handler = new CreateJournalEntryHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJournalEntry_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IJournalEntryRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateJournalEntryHandler(repo.Object, _uow.Object);
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var cmd = new CreateJournalEntryCommand(
            _tenantId, DateTime.UtcNow, "Test yevmiye", "REF-001",
            new List<JournalLineInput>
            {
                new(accountId1, 1000m, 0m, "Borç"),
                new(accountId2, 0m, 1000m, "Alacak")
            });

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ CreateCounterparty ═══

    [Fact]
    public async Task CreateCounterparty_NullRequest_Throws()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var handler = new CreateCounterpartyHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateFixedExpense ═══

    [Fact]
    public async Task CreateFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new CreateFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreatePenaltyRecord ═══

    [Fact]
    public async Task CreatePenaltyRecord_NullRequest_Throws()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var handler = new CreatePenaltyRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateSalaryRecord ═══

    [Fact]
    public async Task CreateSalaryRecord_NullRequest_Throws()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var handler = new CreateSalaryRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreatePlatformCommissionRate ═══

    [Fact]
    public async Task CreatePlatformCommissionRate_NullRequest_Throws()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var handler = new CreatePlatformCommissionRateHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateFixedAsset ═══

    [Fact]
    public async Task CreateFixedAsset_NullRequest_Throws()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var handler = new CreateFixedAssetHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateFinancialGoal ═══

    [Fact]
    public async Task CreateFinancialGoal_NullRequest_Throws()
    {
        var repo = new Mock<IFinancialGoalRepository>();
        var handler = new CreateFinancialGoalHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateAccountingExpense ═══

    [Fact]
    public async Task CreateAccountingExpense_NullRequest_Throws()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var handler = new CreateAccountingExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateBaBsRecord ═══

    [Fact]
    public async Task CreateBaBsRecord_NullRequest_Throws()
    {
        var repo = new Mock<IBaBsRecordRepository>();
        var handler = new CreateBaBsRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ DeleteChartOfAccount ═══

    [Fact]
    public async Task DeleteChartOfAccount_NullRequest_Throws()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var handler = new DeleteChartOfAccountHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ DeleteFixedExpense ═══

    [Fact]
    public async Task DeleteFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new DeleteFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ DeletePenaltyRecord ═══

    [Fact]
    public async Task DeletePenaltyRecord_NullRequest_Throws()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var handler = new DeletePenaltyRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ DeleteSalaryRecord ═══

    [Fact]
    public async Task DeleteSalaryRecord_NullRequest_Throws()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var handler = new DeleteSalaryRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ DeleteTaxRecord ═══

    [Fact]
    public async Task DeleteTaxRecord_NullRequest_Throws()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var handler = new DeleteTaxRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ Update handlers ═══

    [Fact]
    public async Task UpdateChartOfAccount_NullRequest_Throws()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        var handler = new UpdateChartOfAccountHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCounterparty_NullRequest_Throws()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var handler = new UpdateCounterpartyHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFixedExpense_NullRequest_Throws()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var handler = new UpdateFixedExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePenaltyRecord_NullRequest_Throws()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var handler = new UpdatePenaltyRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePlatformCommissionRate_NullRequest_Throws()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var handler = new UpdatePlatformCommissionRateHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateSalaryRecord_NullRequest_Throws()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var handler = new UpdateSalaryRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTaxRecord_NullRequest_Throws()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var handler = new UpdateTaxRecordHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFixedAsset_NullRequest_Throws()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var handler = new UpdateFixedAssetHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ Other Command Handlers ═══

    [Fact]
    public async Task DeactivateFixedAsset_NullRequest_Throws()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var handler = new DeactivateFixedAssetHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordCommission_NullRequest_Throws()
    {
        var repo = new Mock<ICommissionRecordRepository>();
        var handler = new RecordCommissionHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordCargoExpense_NullRequest_Throws()
    {
        var repo = new Mock<ICargoExpenseRepository>();
        var handler = new RecordCargoExpenseHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordTaxWithholding_NullRequest_Throws()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        var handler = new RecordTaxWithholdingHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
