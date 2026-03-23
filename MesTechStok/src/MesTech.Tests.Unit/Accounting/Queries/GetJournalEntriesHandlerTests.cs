using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetJournalEntriesHandler tests — yevmiye kaydi sorgulama, satir toplami ve DTO mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetJournalEntriesHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _repoMock;
    private readonly GetJournalEntriesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetJournalEntriesHandlerTests()
    {
        _repoMock = new Mock<IJournalEntryRepository>();
        _sut = new GetJournalEntriesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsMappedJournalEntries()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var entry = JournalEntry.Create(_tenantId, new DateTime(2026, 1, 15), "Satis fisi", "REF-001");
        entry.AddLine(accountId1, debit: 5000m, credit: 0m, "Kasa giris");
        entry.AddLine(accountId2, debit: 0m, credit: 5000m, "Gelir hesabi");

        var query = new GetJournalEntriesQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry }.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Satis fisi");
        result[0].ReferenceNumber.Should().Be("REF-001");
        result[0].TotalDebit.Should().Be(5000m);
        result[0].TotalCredit.Should().Be(5000m);
        result[0].Lines.Should().HaveCount(2);
        result[0].Lines[0].Debit.Should().Be(5000m);
        result[0].Lines[1].Credit.Should().Be(5000m);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 6, 30);
        var query = new GetJournalEntriesQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>().AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultipleEntries_MapsAllWithCorrectLineTotals()
    {
        // Arrange
        var from = new DateTime(2026, 2, 1);
        var to = new DateTime(2026, 2, 28);
        var accA = Guid.NewGuid();
        var accB = Guid.NewGuid();

        var entry1 = JournalEntry.Create(_tenantId, new DateTime(2026, 2, 5), "Alis fisi", "REF-100");
        entry1.AddLine(accA, debit: 1000m, credit: 0m);
        entry1.AddLine(accB, debit: 0m, credit: 1000m);

        var entry2 = JournalEntry.Create(_tenantId, new DateTime(2026, 2, 20), "Gider fisi", "REF-101");
        entry2.AddLine(accA, debit: 0m, credit: 3000m);
        entry2.AddLine(accB, debit: 3000m, credit: 0m);

        var query = new GetJournalEntriesQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry1, entry2 }.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].TotalDebit.Should().Be(1000m);
        result[0].TotalCredit.Should().Be(1000m);
        result[1].TotalDebit.Should().Be(3000m);
        result[1].TotalCredit.Should().Be(3000m);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
