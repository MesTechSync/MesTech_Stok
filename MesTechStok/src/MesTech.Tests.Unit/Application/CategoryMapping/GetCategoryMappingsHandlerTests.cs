using FluentAssertions;
using MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.CategoryMapping;

[Trait("Category", "Unit")]
public class GetCategoryMappingsHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<ICategoryPlatformMappingRepository> _mappingRepo = new();

    private GetCategoryMappingsHandler CreateHandler() =>
        new(_categoryRepo.Object, _mappingRepo.Object);

    [Fact]
    public async Task Handle_CategoriesWithMappings_ShouldReturnMappedView()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var mappingId = Guid.NewGuid();

        var cat = new Category { Name = "Elektronik", TenantId = tenantId };
        EntityTestHelper.SetEntityId(cat, catId);
        var categories = new List<Category> { cat };

        var mapping = new CategoryPlatformMapping
        {
            CategoryId = catId,
            ExternalCategoryId = "1001",
            ExternalCategoryName = "Electronics",
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol
        };

        EntityTestHelper.SetEntityId(mapping, mappingId);

        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _mappingRepo
            .Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<PlatformType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryPlatformMapping> { mapping });

        var handler = CreateHandler();
        var query = new GetCategoryMappingsQuery(tenantId, PlatformType.Trendyol);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsMapped.Should().BeTrue();
        result[0].PlatformCategoryId.Should().Be("1001");
        result[0].InternalCategoryName.Should().Be("Elektronik");
    }

    [Fact]
    public async Task Handle_NoCategoriesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Category>());
        _mappingRepo
            .Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryPlatformMapping>());

        var handler = CreateHandler();
        var query = new GetCategoryMappingsQuery(tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CategoryWithoutMapping_ShouldReturnUnmapped()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var catId = Guid.NewGuid();

        var cat = new Category { Name = "Gida", TenantId = tenantId };
        EntityTestHelper.SetEntityId(cat, catId);
        var categories = new List<Category> { cat };

        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _mappingRepo
            .Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryPlatformMapping>());

        var handler = CreateHandler();
        var query = new GetCategoryMappingsQuery(tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsMapped.Should().BeFalse();
        result[0].PlatformCategoryId.Should().BeNull();
        result[0].MappingId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
