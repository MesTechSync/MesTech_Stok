using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCredentialsSettingsHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepoMock = new();
    private readonly GetCredentialsSettingsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCredentialsSettingsHandlerTests()
    {
        _sut = new GetCredentialsSettingsHandler(_storeRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WithActiveStores_ReturnsPlatforms()
    {
        var stores = new List<Store>
        {
            new() { Id = Guid.NewGuid(), PlatformType = PlatformType.Trendyol, IsActive = true },
            new() { Id = Guid.NewGuid(), PlatformType = PlatformType.Hepsiburada, IsActive = true },
            new() { Id = Guid.NewGuid(), PlatformType = PlatformType.N11, IsActive = false }
        };
        _storeRepoMock.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stores.AsReadOnly());

        var query = new GetCredentialsSettingsQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.ConfiguredPlatforms.Should().HaveCount(2);
        result.ConfiguredPlatforms.Should().Contain("Trendyol");
        result.ConfiguredPlatforms.Should().Contain("Hepsiburada");
    }

    [Fact]
    public async Task Handle_NoStores_ReturnsEmpty()
    {
        _storeRepoMock.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var query = new GetCredentialsSettingsQuery(_tenantId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.ConfiguredPlatforms.Should().BeEmpty();
    }
}
