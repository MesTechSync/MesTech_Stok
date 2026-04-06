using FluentAssertions;
using MesTech.Application.Queries.GetBrandById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Brand")]
public class GetBrandByIdHandlerTests
{
    private readonly Mock<IBrandRepository> _brandRepo = new();

    private GetBrandByIdHandler CreateSut() => new(_brandRepo.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new GetBrandByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_BrandExists_ShouldReturnResult()
    {
        var brandId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var brand = Brand.Create(tenantId, "Samsung", "https://cdn.example.com/samsung.png");
        typeof(MesTech.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(brand, brandId);

        _brandRepo.Setup(r => r.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);

        var sut = CreateSut();
        var result = await sut.Handle(new GetBrandByIdQuery(brandId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Samsung");
        result.TenantId.Should().Be(tenantId);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BrandNotFound_ShouldReturnNull()
    {
        var brandId = Guid.NewGuid();
        _brandRepo.Setup(r => r.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new GetBrandByIdQuery(brandId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        var brandId = Guid.NewGuid();
        _brandRepo.Setup(r => r.GetByIdAsync(brandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var sut = CreateSut();
        await sut.Handle(new GetBrandByIdQuery(brandId), CancellationToken.None);

        _brandRepo.Verify(r => r.GetByIdAsync(brandId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
