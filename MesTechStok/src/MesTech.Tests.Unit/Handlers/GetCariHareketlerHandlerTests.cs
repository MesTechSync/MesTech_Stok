using FluentAssertions;
using MesTech.Application.Queries.GetCariHareketler;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCariHareketlerHandlerTests
{
    private readonly Mock<ICariHareketRepository> _repo;
    private readonly GetCariHareketlerHandler _sut;

    public GetCariHareketlerHandlerTests()
    {
        _repo = new Mock<ICariHareketRepository>();
        _sut = new GetCariHareketlerHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithoutDateRange_CallsGetByCariHesapId()
    {
        var hesapId = Guid.NewGuid();
        _repo.Setup(r => r.GetByCariHesapIdAsync(hesapId))
            .ReturnsAsync(new List<CariHareket>().AsReadOnly());

        var query = new GetCariHareketlerQuery(hesapId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByCariHesapIdAsync(hesapId), Times.Once());
        _repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never());
    }

    [Fact]
    public async Task Handle_WithDateRange_CallsGetByDateRange()
    {
        var hesapId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;
        _repo.Setup(r => r.GetByDateRangeAsync(hesapId, from, to))
            .ReturnsAsync(new List<CariHareket>().AsReadOnly());

        var query = new GetCariHareketlerQuery(hesapId, from, to);

        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetByDateRangeAsync(hesapId, from, to), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
