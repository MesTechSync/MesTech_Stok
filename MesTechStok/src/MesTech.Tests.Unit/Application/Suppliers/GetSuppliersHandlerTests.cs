using FluentAssertions;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Suppliers;

[Trait("Category", "Unit")]
[Trait("Feature", "Suppliers")]
public class GetSuppliersHandlerTests
{
    [Fact]
    public async Task Handle_ActiveOnly_ShouldCallGetActive()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new()
            {
                Name = "Supplier A", Code = "SA-001",
                IsActive = true, ContactPerson = "Ali", Email = "ali@sa.com"
            }
        }.AsReadOnly();

        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers);

        var handler = new GetSuppliersHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetSuppliersQuery(ActiveOnly: true), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Supplier A");
        result[0].Code.Should().Be("SA-001");
        result[0].IsActive.Should().BeTrue();
        mockRepo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AllSuppliers_ShouldCallGetAll()
    {
        // Arrange
        var suppliers = new List<Supplier>
        {
            new() { Name = "Active", Code = "A-001", IsActive = true },
            new() { Name = "Inactive", Code = "I-001", IsActive = false }
        }.AsReadOnly();

        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers);

        var handler = new GetSuppliersHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetSuppliersQuery(ActiveOnly: false), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        mockRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockRepo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = new GetSuppliersHandler(Mock.Of<ISupplierRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_EmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var mockRepo = new Mock<ISupplierRepository>();
        mockRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Supplier>().AsReadOnly());

        var handler = new GetSuppliersHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetSuppliersQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
