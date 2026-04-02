using FluentAssertions;
using MesTech.Application.Features.CategoryMapping.Commands.MapCategory;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// MapCategoryHandler unit testleri.
/// Yeni eslestirme olusturma ve mevcut eslestirme guncelleme senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class MapCategoryHandlerTests
{
    private readonly Mock<ICategoryPlatformMappingRepository> _mappingRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MapCategoryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public MapCategoryHandlerTests()
    {
        _handler = new MapCategoryHandler(
            _mappingRepoMock.Object,
            _uowMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Yeni eslestirme olusturulmali
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task MapCategory_CreatesMapping()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        _mappingRepoMock
            .Setup(r => r.GetByCategoryAndPlatformAsync(
                _tenantId, categoryId, PlatformType.Trendyol, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryPlatformMapping?)null); // no existing mapping

        var command = new MapCategoryCommand(
            _tenantId,
            categoryId,
            PlatformType.Trendyol,
            "12345",
            "Elektronik > Telefon");

        // Act
        var resultId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().NotBe(Guid.Empty);

        _mappingRepoMock.Verify(
            r => r.AddAsync(
                It.Is<CategoryPlatformMapping>(m =>
                    m.TenantId == _tenantId &&
                    m.CategoryId == categoryId &&
                    m.PlatformType == PlatformType.Trendyol &&
                    m.ExternalCategoryId == "12345" &&
                    m.ExternalCategoryName == "Elektronik > Telefon"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
