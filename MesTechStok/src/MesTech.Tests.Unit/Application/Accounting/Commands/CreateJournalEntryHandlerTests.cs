using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateJournalEntryHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateJournalEntryHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateJournalEntryHandlerTests()
    {
        _sut = new CreateJournalEntryHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_BalancedEntry_ShouldCreateAndReturnId()
    {
        // Arrange
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        var lines = new List<JournalLineInput>
        {
            new(accountId1, 1000m, 0m, "Kasa"),
            new(accountId2, 0m, 1000m, "Banka")
        };
        var command = new CreateJournalEntryCommand(
            TenantId, DateTime.Today, "Kasadan bankaya transfer", "REF-001", lines);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnbalancedEntry_ShouldThrow()
    {
        // Arrange — debit != credit
        var lines = new List<JournalLineInput>
        {
            new(Guid.NewGuid(), 1000m, 0m, "Kasa"),
            new(Guid.NewGuid(), 0m, 500m, "Banka")
        };
        var command = new CreateJournalEntryCommand(
            TenantId, DateTime.Today, "Dengesiz kayit", null, lines);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert — JournalEntry.Validate() should throw for unbalanced
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
