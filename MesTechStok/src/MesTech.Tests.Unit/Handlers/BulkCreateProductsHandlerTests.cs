using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkCreateProducts;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class BulkCreateProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<BulkCreateProductsHandler>> _loggerMock = new();
    private readonly BulkCreateProductsHandler _sut;

    public BulkCreateProductsHandlerTests()
    {
        _sut = new BulkCreateProductsHandler(
            _productRepoMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProducts_CreatesAllAndReturnsCounts()
    {
        // Arrange
        var products = new List<BulkProductInput>
        {
            new() { Name = "Product A", SKU = "SKU-A", Price = 10m, Quantity = 5 },
            new() { Name = "Product B", SKU = "SKU-B", Price = 20m, Quantity = 3 }
        };

        var command = new BulkCreateProductsCommand(Guid.NewGuid(), products);

        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Product>());

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.TotalReceived.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailCount.Should().Be(0);
        result.Errors.Should().BeEmpty();

        _productRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Domain.Entities.Product>()),
            Times.Exactly(2));

        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSKUInDatabase_ReportsError()
    {
        // Arrange
        var existingProduct = new Domain.Entities.Product { SKU = "SKU-DUP", Name = "Existing" };

        var products = new List<BulkProductInput>
        {
            new() { Name = "New Product", SKU = "SKU-DUP", Price = 15m }
        };

        var command = new BulkCreateProductsCommand(Guid.NewGuid(), products);

        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Product> { existingProduct });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("SKU-DUP")
            .And.Contain("zaten mevcut");

        _productRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Domain.Entities.Product>()),
            Times.Never);

        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_IntraBatchDuplicateSKU_ReportsErrorForSecondOccurrence()
    {
        // Arrange
        var products = new List<BulkProductInput>
        {
            new() { Name = "First", SKU = "SKU-SAME", Price = 10m },
            new() { Name = "Second", SKU = "SKU-SAME", Price = 20m }
        };

        var command = new BulkCreateProductsCommand(Guid.NewGuid(), products);

        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Product>());

        _uowMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("tekrar ediyor");
    }

    [Fact]
    public async Task Handle_EmptyNameAndSKU_ReportsValidationErrors()
    {
        // Arrange
        var products = new List<BulkProductInput>
        {
            new() { Name = "", SKU = "SKU-OK", Price = 10m },
            new() { Name = "Valid", SKU = "", Price = 20m },
            new() { Name = "  ", SKU = "SKU-OK2", Price = 30m }
        };

        var command = new BulkCreateProductsCommand(Guid.NewGuid(), products);

        _productRepoMock
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Product>());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(3);
        result.Errors.Should().HaveCount(3);
        result.Errors[0].Should().Contain("Urun adi bos");
        result.Errors[1].Should().Contain("SKU bos");
        result.Errors[2].Should().Contain("Urun adi bos");

        _uowMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
