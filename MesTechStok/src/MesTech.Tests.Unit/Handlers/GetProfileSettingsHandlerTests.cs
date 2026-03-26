using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetProfileSettingsHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly GetProfileSettingsHandler _sut;

    public GetProfileSettingsHandlerTests()
    {
        _sut = new GetProfileSettingsHandler(_tenantRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingTenant_ReturnsProfile()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Ltd.", TaxNumber = "1234567890", IsActive = true };
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var query = new GetProfileSettingsQuery(tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TenantName.Should().Be("Test Ltd.");
        result.TaxNumber.Should().Be("1234567890");
    }

    [Fact]
    public async Task Handle_NonExistentTenant_ReturnsNull()
    {
        _tenantRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);

        var query = new GetProfileSettingsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }
}
