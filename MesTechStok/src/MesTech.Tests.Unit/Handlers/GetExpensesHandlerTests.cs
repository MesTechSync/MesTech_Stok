using FluentAssertions;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetExpensesHandlerTests
{
    private readonly Mock<IExpenseRepository> _repo;
    private readonly GetExpensesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetExpensesHandlerTests()
    {
        _repo = new Mock<IExpenseRepository>();
        _sut = new GetExpensesHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithType_CallsGetByType()
    {
        _repo.Setup(r => r.GetByTypeAsync(ExpenseType.Kargo, _tenantId))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var query = new GetExpensesQuery(Type: ExpenseType.Kargo, TenantId: _tenantId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByTypeAsync(ExpenseType.Kargo, _tenantId), Times.Once());
    }

    [Fact]
    public async Task Handle_WithDateRange_CallsGetByDateRange()
    {
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;
        _repo.Setup(r => r.GetByDateRangeAsync(from, to, _tenantId))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var query = new GetExpensesQuery(from, to, TenantId: _tenantId);

        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetByDateRangeAsync(from, to, _tenantId), Times.Once());
    }

    [Fact]
    public async Task Handle_NoFilter_CallsGetAll()
    {
        _repo.Setup(r => r.GetAllAsync(_tenantId))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var query = new GetExpensesQuery(TenantId: _tenantId);

        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetAllAsync(_tenantId), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
