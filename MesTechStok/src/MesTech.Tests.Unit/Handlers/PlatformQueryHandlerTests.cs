using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for platform-related query handlers.
/// </summary>
[Trait("Category", "Unit")]
public class PlatformQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ GetPlatformCommissionRatesHandler ═══════

    [Fact]
    public async Task GetPlatformCommissionRates_CallsRepository()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        repo.Setup(r => r.GetByPlatformAsync(_tenantId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformCommission>().AsReadOnly());

        var sut = new GetPlatformCommissionRatesHandler(repo.Object);
        var result = await sut.Handle(
            new GetPlatformCommissionRatesQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
    }

    // ═══════ GetPlatformHealthHandler ═══════

    [Fact]
    public async Task GetPlatformHealth_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ISyncLogRepository>();
        var sut = new GetPlatformHealthHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
