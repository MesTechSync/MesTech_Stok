using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.Dropshipping;

[Trait("Category", "Unit")]
public class GetDropshipSuppliersHandlerTests
{
    private readonly Mock<IDropshipSupplierRepository> _repository = new();

    private GetDropshipSuppliersHandler CreateHandler() =>
        new(_repository.Object);

    [Fact]
    public async Task Handle_SuppliersExist_ShouldReturnMappedDtos()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier1 = DropshipSupplier.Create(
            tenantId, "Supplier A", "https://a.com",
            DropshipMarkupType.Percentage, 10m);
        var supplier2 = DropshipSupplier.Create(
            tenantId, "Supplier B", null,
            DropshipMarkupType.FixedAmount, 5m);

        _repository
            .Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier> { supplier1, supplier2 });

        var handler = CreateHandler();
        var query = new GetDropshipSuppliersQuery(tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Supplier A");
        result[0].MarkupType.Should().Be("Percentage");
        result[0].MarkupValue.Should().Be(10m);
        result[1].Name.Should().Be("Supplier B");
    }

    [Fact]
    public async Task Handle_NoSuppliersForTenant_ShouldReturnEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier>());

        var handler = CreateHandler();
        var query = new GetDropshipSuppliersQuery(tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SupplierWithApiCredentials_ShouldMapCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier = DropshipSupplier.Create(
            tenantId, "API Supplier", "https://api.supplier.com",
            DropshipMarkupType.Percentage, 20m);
        supplier.SetApiCredentials("https://api.endpoint.com", "secret-key");
        supplier.EnableAutoSync(30);

        _repository
            .Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier> { supplier });

        var handler = CreateHandler();
        var query = new GetDropshipSuppliersQuery(tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var dto = result[0];
        dto.ApiEndpoint.Should().Be("https://api.endpoint.com");
        dto.AutoSync.Should().BeTrue();
        dto.SyncIntervalMinutes.Should().Be(30);
        dto.IsActive.Should().BeTrue();
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
