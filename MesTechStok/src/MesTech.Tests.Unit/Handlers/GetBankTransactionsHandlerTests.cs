using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetBankTransactionsHandlerTests
{
    private readonly Mock<IBankTransactionRepository> _repoMock = new();
    private readonly GetBankTransactionsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    public GetBankTransactionsHandlerTests()
    {
        _sut = new GetBankTransactionsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsTransactions()
    {
        var transactions = new List<BankTransaction>
        {
            new(Guid.NewGuid(), _bankAccountId, 1500m, "Trendyol havale", DateTime.UtcNow) { IsReconciled = false },
            new(Guid.NewGuid(), _bankAccountId, -300m, "Kargo ödemesi", DateTime.UtcNow) { IsReconciled = true }
        };
        _repoMock.Setup(r => r.GetByBankAccountAsync(_tenantId, _bankAccountId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.AsReadOnly());

        var query = new GetBankTransactionsQuery(_tenantId, _bankAccountId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Amount.Should().Be(1500m);
        result[1].IsReconciled.Should().BeTrue();
        _repoMock.Verify(r => r.GetByBankAccountAsync(_tenantId, _bankAccountId, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByBankAccountAsync(_tenantId, _bankAccountId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankTransaction>().AsReadOnly());

        var query = new GetBankTransactionsQuery(_tenantId, _bankAccountId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
