using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetPlatformCommissionRatesHandlerTests
{
    private readonly Mock<IPlatformCommissionRepository> _repoMock = new();
    private readonly GetPlatformCommissionRatesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetPlatformCommissionRatesHandlerTests()
    {
        _sut = new GetPlatformCommissionRatesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsCommissionRates()
    {
        var commissions = new List<PlatformCommission>
        {
            new PlatformCommission { Id = Guid.NewGuid(), Platform = PlatformType.Trendyol, Rate = 12.5m, IsActive = true, Currency = "TRY" }
        };
        _repoMock.Setup(r => r.GetByPlatformAsync(_tenantId, PlatformType.Trendyol, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(commissions.AsReadOnly());

        var query = new GetPlatformCommissionRatesQuery(_tenantId, PlatformType.Trendyol);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Rate.Should().Be(12.5m);
        result[0].Platform.Should().Be("Trendyol");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
