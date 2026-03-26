using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetGeneralSettingsHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly GetGeneralSettingsHandler _sut;

    public GetGeneralSettingsHandlerTests()
    {
        _sut = new GetGeneralSettingsHandler(_tenantRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingTenant_ReturnsSettings()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Firma" };
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var query = new GetGeneralSettingsQuery(tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TenantName.Should().Be("Test Firma");
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task Handle_NonExistentTenant_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var query = new GetGeneralSettingsQuery(tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }
}
