using FluentAssertions;
using MesTech.Application.Queries.GetIncomes;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetIncomesHandlerTests
{
    private readonly Mock<IIncomeRepository> _repo;
    private readonly GetIncomesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetIncomesHandlerTests()
    {
        _repo = new Mock<IIncomeRepository>();
        _sut = new GetIncomesHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithType_CallsGetByType()
    {
        _repo.Setup(r => r.GetByTypeAsync(IncomeType.Satis, _tenantId))
            .ReturnsAsync(new List<Income>().AsReadOnly());

        var query = new GetIncomesQuery(Type: IncomeType.Satis, TenantId: _tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByTypeAsync(IncomeType.Satis, _tenantId), Times.Once());
    }

    [Fact]
    public async Task Handle_WithDateRange_CallsGetByDateRange()
    {
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;
        _repo.Setup(r => r.GetByDateRangeAsync(from, to, _tenantId))
            .ReturnsAsync(new List<Income>().AsReadOnly());

        var query = new GetIncomesQuery(from, to, TenantId: _tenantId);
        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetByDateRangeAsync(from, to, _tenantId), Times.Once());
    }

    [Fact]
    public async Task Handle_NoFilter_CallsGetAll()
    {
        _repo.Setup(r => r.GetAllAsync(_tenantId))
            .ReturnsAsync(new List<Income>().AsReadOnly());

        var query = new GetIncomesQuery(TenantId: _tenantId);
        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetAllAsync(_tenantId), Times.Once());
    }
}
