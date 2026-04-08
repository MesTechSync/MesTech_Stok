using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// GL Transaction handler tests — comprehensive coverage for JournalEntry-based GL operations.
/// NOTE: GLTransaction concept is implemented via JournalEntry handlers (Create/Update/Get).
/// There is no separate GLTransaction entity — JournalEntry IS the general ledger transaction.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GLTransactionHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    // CreateJournalEntryHandler — edge cases not covered elsewhere
    // ═══════════════════════════════════════════════════════════

    private CreateJournalEntryHandler CreateSut()
        => new(_repoMock.Object, _uowMock.Object);

    [Fact]
    public async Task Create_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Create_EmptyTenantId_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            Guid.Empty,
            DateTime.UtcNow,
            "Test entry",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, "Debit"),
                new(Guid.NewGuid(), 0m, 100m, "Credit")
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Tenant*");
    }

    [Fact]
    public async Task Create_EmptyDescription_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Create_WhitespaceDescription_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "   ",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Create_LineWithBothDebitAndCredit_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Invalid line",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 500m, 500m, "Both set"), // invalid
                new(Guid.NewGuid(), 0m, 1000m, null)
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*both debit and credit*");
    }

    [Fact]
    public async Task Create_LineWithEmptyAccountId_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Empty account",
            null,
            new List<JournalLineInput>
            {
                new(Guid.Empty, 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Account*");
    }

    [Fact]
    public async Task Create_LineWithZeroDebitAndZeroCredit_ThrowsArgumentException()
    {
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "Zero amounts",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 0m, 0m, null), // both zero
                new(Guid.NewGuid(), 0m, 100m, null)
            });

        var act = () => CreateSut().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*debit or credit must be greater*");
    }

    [Fact]
    public async Task Create_WithReferenceNumber_PersistsReferenceNumber()
    {
        var accA = Guid.NewGuid();
        var accB = Guid.NewGuid();
        var command = new CreateJournalEntryCommand(
            _tenantId,
            DateTime.UtcNow,
            "With reference",
            "REF-2026-042",
            new List<JournalLineInput>
            {
                new(accA, 250m, 0m, null),
                new(accB, 0m, 250m, null)
            });

        JournalEntry? captured = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e);

        await CreateSut().Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ReferenceNumber.Should().Be("REF-2026-042");
    }

    // ═══════════════════════════════════════════════════════════
    // UpdateJournalEntryHandler — happy path + concurrency tests
    // ═══════════════════════════════════════════════════════════

    private UpdateJournalEntryHandler UpdateSut()
        => new(_repoMock.Object, _uowMock.Object);

    [Fact]
    public async Task Update_HappyPath_ReturnsSuccess()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow.AddDays(-1), "Original");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var newAccA = Guid.NewGuid();
        var newAccB = Guid.NewGuid();
        var cmd = new UpdateJournalEntryCommand(
            entry.Id,
            _tenantId,
            DateTime.UtcNow,
            "Updated description",
            "REF-UPD-001",
            new List<JournalLineInput>
            {
                new(newAccA, 2000m, 0m, "New debit"),
                new(newAccB, 0m, 2000m, "New credit")
            },
            null);

        var result = await UpdateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ConcurrencyConflict_ReturnsFailure()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Original");
        entry.RowVersion = new byte[] { 1, 2, 3, 4 };
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var staleRowVersion = new byte[] { 9, 9, 9, 9 }; // different from DB
        var cmd = new UpdateJournalEntryCommand(
            entry.Id,
            _tenantId,
            DateTime.UtcNow,
            "Concurrent update",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            },
            staleRowVersion);

        var result = await UpdateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Concurrency conflict");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_MatchingRowVersion_Succeeds()
    {
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Original");
        entry.RowVersion = rowVersion;
        entry.AddLine(Guid.NewGuid(), 500m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var cmd = new UpdateJournalEntryCommand(
            entry.Id,
            _tenantId,
            DateTime.UtcNow,
            "Updated with matching version",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 500m, 0m, null),
                new(Guid.NewGuid(), 0m, 500m, null)
            },
            (byte[])rowVersion.Clone());

        var result = await UpdateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_NullRowVersionOnBothSides_Succeeds()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "No version");
        entry.RowVersion = null;
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var cmd = new UpdateJournalEntryCommand(
            entry.Id,
            _tenantId,
            DateTime.UtcNow,
            "Updated",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 100m, null)
            },
            null);

        var result = await UpdateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_UnbalancedLines_ThrowsImbalanceException()
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Original");
        entry.AddLine(Guid.NewGuid(), 100m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 100m);

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var cmd = new UpdateJournalEntryCommand(
            entry.Id,
            _tenantId,
            DateTime.UtcNow,
            "Unbalanced update",
            null,
            new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 999m, 0m, null),
                new(Guid.NewGuid(), 0m, 500m, null) // 999 != 500
            },
            null);

        var act = () => UpdateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<JournalEntryImbalanceException>();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════
    // GetJournalEntriesHandler — additional edge cases
    // ═══════════════════════════════════════════════════════════

    private GetJournalEntriesHandler QuerySut()
        => new(_repoMock.Object);

    [Fact]
    public async Task Get_PostedEntry_MapsIsPostedCorrectly()
    {
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31);
        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 3, 15), "Posted entry", "REF-POST");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 1000m);
        entry.Post();

        _repoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry }.AsReadOnly());

        var result = await QuerySut().Handle(
            new GetJournalEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsPosted.Should().BeTrue();
        result[0].PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_UnpostedEntry_MapsIsPostedAsFalse()
    {
        var from = new DateTime(2026, 4, 1);
        var to = new DateTime(2026, 4, 30);
        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 4, 10), "Draft entry");
        entry.AddLine(Guid.NewGuid(), 500m, 0m);
        entry.AddLine(Guid.NewGuid(), 0m, 500m);

        _repoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry }.AsReadOnly());

        var result = await QuerySut().Handle(
            new GetJournalEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsPosted.Should().BeFalse();
        result[0].PostedAt.Should().BeNull();
    }

    [Fact]
    public async Task Get_EntryWithThreeLines_MapsAllLinesToDto()
    {
        var from = new DateTime(2026, 5, 1);
        var to = new DateTime(2026, 5, 31);
        var accA = Guid.NewGuid();
        var accB = Guid.NewGuid();
        var accC = Guid.NewGuid();

        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 5, 20), "Compound entry");
        entry.AddLine(accA, 300m, 0m, "Line A");
        entry.AddLine(accB, 200m, 0m, "Line B");
        entry.AddLine(accC, 0m, 500m, "Line C");

        _repoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry }.AsReadOnly());

        var result = await QuerySut().Handle(
            new GetJournalEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.TotalDebit.Should().Be(500m);
        dto.TotalCredit.Should().Be(500m);
        dto.Lines.Should().HaveCount(3);
        dto.Lines.Sum(l => l.Debit).Should().Be(500m);
        dto.Lines.Sum(l => l.Credit).Should().Be(500m);
    }

    [Fact]
    public async Task Get_RepositoryCalledWithExactTenantAndDateRange()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);

        _repoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>().AsReadOnly());

        await QuerySut().Handle(
            new GetJournalEntriesQuery(_tenantId, from, to), CancellationToken.None);

        _repoMock.Verify(
            r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
