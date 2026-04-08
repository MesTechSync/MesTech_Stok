using FluentAssertions;
using MesTech.Application.Features.CategoryMapping.Commands.MapCategory;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.CategoryMapping;

[Trait("Category", "Unit")]
public class MapCategoryHandlerTests
{
    private readonly Mock<ICategoryPlatformMappingRepository> _mappingRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MapCategoryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public MapCategoryHandlerTests()
        => _sut = new MapCategoryHandler(_mappingRepoMock.Object, _uowMock.Object);

    [Fact]
    public async Task Handle_NewMapping_CreatesAndReturnsGuid()
    {
        _mappingRepoMock.Setup(r => r.GetByCategoryAndPlatformAsync(
            _tenantId, It.IsAny<Guid>(), It.IsAny<PlatformType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryPlatformMapping?)null);

        var cmd = new MapCategoryCommand(_tenantId, Guid.NewGuid(), PlatformType.Trendyol, "123", "Elektronik");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _mappingRepoMock.Verify(r => r.AddAsync(It.IsAny<CategoryPlatformMapping>(), It.IsAny<CancellationToken>()), Times.Once());
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ExistingMapping_UpdatesAndReturnsExistingId()
    {
        var existingId = Guid.NewGuid();
        var existing = new CategoryPlatformMapping
        {
            Id = existingId,
            TenantId = _tenantId,
            ExternalCategoryId = "OLD",
            ExternalCategoryName = "Old Name"
        };
        _mappingRepoMock.Setup(r => r.GetByCategoryAndPlatformAsync(
            _tenantId, It.IsAny<Guid>(), It.IsAny<PlatformType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var cmd = new MapCategoryCommand(_tenantId, Guid.NewGuid(), PlatformType.Trendyol, "NEW-123", "Yeni Kategori");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(existingId);
        existing.ExternalCategoryId.Should().Be("NEW-123");
        _mappingRepoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
