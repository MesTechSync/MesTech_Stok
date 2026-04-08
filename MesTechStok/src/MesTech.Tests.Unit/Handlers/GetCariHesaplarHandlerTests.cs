using FluentAssertions;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCariHesaplarHandlerTests
{
    private readonly Mock<ICariHesapRepository> _repo;
    private readonly GetCariHesaplarHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCariHesaplarHandlerTests()
    {
        _repo = new Mock<ICariHesapRepository>();
        _sut = new GetCariHesaplarHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_WithType_CallsGetByType()
    {
        _repo.Setup(r => r.GetByTypeAsync(CariHesapType.Musteri, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CariHesap>().AsReadOnly());

        var query = new GetCariHesaplarQuery(CariHesapType.Musteri, _tenantId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByTypeAsync(CariHesapType.Musteri, _tenantId, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithoutType_CallsGetAll()
    {
        _repo.Setup(r => r.GetAllAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CariHesap>().AsReadOnly());

        var query = new GetCariHesaplarQuery(null, _tenantId);

        await _sut.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetAllAsync(_tenantId, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
