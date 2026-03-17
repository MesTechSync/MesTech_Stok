using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// ImportBankStatementHandler tests — valid import, idempotency, and empty data scenarios.
/// </summary>
[Trait("Category", "Unit")]
public class ImportBankStatementHandlerTests
{
    private readonly Mock<IBankTransactionRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ImportBankStatementHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    public ImportBankStatementHandlerTests()
    {
        _repoMock = new Mock<IBankTransactionRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new ImportBankStatementHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidTransactions_ReturnsImportCount()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankTransaction?)null);

        var command = new ImportBankStatementCommand(
            _tenantId,
            _bankAccountId,
            new List<BankTransactionInput>
            {
                new(DateTime.UtcNow, 1500m, "HAVALE GELEN", "REF-001", "KEY-001"),
                new(DateTime.UtcNow, -200m, "EFT GIDEN", "REF-002", "KEY-002"),
                new(DateTime.UtcNow, 3000m, "TRENDYOL ODEME", null, "KEY-003")
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(3);

        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<BankTransaction>>(list => list.Count() == 3),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyTransactionList_ReturnsZeroAndDoesNotSave()
    {
        // Arrange
        var command = new ImportBankStatementCommand(
            _tenantId,
            _bankAccountId,
            new List<BankTransactionInput>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);

        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<BankTransaction>>(), It.IsAny<CancellationToken>()),
            Times.Never());

        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_SkipsDuplicate()
    {
        // Arrange — first transaction has existing key, second is new
        var existingTx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow, 1000m, "EXISTING", idempotencyKey: "KEY-DUP");

        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(
                _tenantId, "KEY-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTx);

        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(
                _tenantId, "KEY-NEW", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankTransaction?)null);

        var command = new ImportBankStatementCommand(
            _tenantId,
            _bankAccountId,
            new List<BankTransactionInput>
            {
                new(DateTime.UtcNow, 1000m, "DUPLICATE TX", null, "KEY-DUP"),
                new(DateTime.UtcNow, 2000m, "NEW TX", null, "KEY-NEW")
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — only 1 new transaction imported
        result.Should().Be(1);

        _repoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<BankTransaction>>(list => list.Count() == 1),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_AllDuplicates_ReturnsZeroAndDoesNotSave()
    {
        // Arrange — all transactions are duplicates
        var existingTx = BankTransaction.Create(
            _tenantId, _bankAccountId, DateTime.UtcNow, 500m, "EXISTS", idempotencyKey: "KEY-1");

        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(
                _tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTx);

        var command = new ImportBankStatementCommand(
            _tenantId,
            _bankAccountId,
            new List<BankTransactionInput>
            {
                new(DateTime.UtcNow, 500m, "DUP 1", null, "KEY-1"),
                new(DateTime.UtcNow, 700m, "DUP 2", null, "KEY-2")
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);

        _repoMock.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<BankTransaction>>(), It.IsAny<CancellationToken>()),
            Times.Never());

        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_TransactionWithoutIdempotencyKey_SkipsCheck()
    {
        // Arrange — transaction without idempotency key should be imported without check
        var command = new ImportBankStatementCommand(
            _tenantId,
            _bankAccountId,
            new List<BankTransactionInput>
            {
                new(DateTime.UtcNow, 1500m, "NO KEY TX", null, null)
            });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);

        // Verify that GetByIdempotencyKeyAsync was never called (no key to check)
        _repoMock.Verify(
            r => r.GetByIdempotencyKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
