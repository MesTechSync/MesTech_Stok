using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.DTOs;
using MesTech.Application.Queries.GetProductById;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
[Trait("Feature", "MediatRBehavior")]
[Trait("Phase", "Dalga3")]
public class BehaviorTests
{
    // ───────────────────────── LoggingBehavior ─────────────────────────

    [Fact]
    public async Task LoggingBehavior_Handle_ExecutesNextDelegate_ReturnsResponse()
    {
        // Arrange
        var behavior = new LoggingBehavior<CreateProductCommand, CreateProductResult>();
        var expectedResponse = new CreateProductResult { IsSuccess = true, ProductId = Guid.NewGuid() };
        var command = new CreateProductCommand("Test", "SKU-001", null, 10m, 20m, Guid.NewGuid());

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task LoggingBehavior_Handle_WhenNextThrows_ExceptionPropagates()
    {
        // Arrange
        var behavior = new LoggingBehavior<CreateProductCommand, CreateProductResult>();
        var command = new CreateProductCommand("Test", "SKU-002", null, 10m, 20m, Guid.NewGuid());

        RequestHandlerDelegate<CreateProductResult> next = () => throw new InvalidOperationException("Pipeline error");

        // Act
        Func<Task> act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Pipeline error");
    }

    [Fact]
    public async Task LoggingBehavior_Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var behavior = new LoggingBehavior<CreateProductCommand, CreateProductResult>();
        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(new CreateProductResult());

        // Act
        Func<Task> act = () => behavior.Handle(null!, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    // ───────────────────────── TransactionBehavior ─────────────────────────

    [Fact]
    public async Task TransactionBehavior_Handle_Command_DelegatesToNext()
    {
        // Arrange — TransactionBehavior is now a pass-through (no explicit transaction)
        var behavior = new TransactionBehavior<CreateProductCommand, CreateProductResult>();
        var command = new CreateProductCommand("Test", "SKU-003", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true };
        var nextCalled = false;

        RequestHandlerDelegate<CreateProductResult> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionBehavior_Handle_Command_WhenExceptionThrown_Propagates()
    {
        // Arrange
        var behavior = new TransactionBehavior<CreateProductCommand, CreateProductResult>();
        var command = new CreateProductCommand("Test", "SKU-004", null, 10m, 20m, Guid.NewGuid());

        RequestHandlerDelegate<CreateProductResult> next = () => throw new InvalidOperationException("DB failure");

        // Act
        Func<Task> act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB failure");
    }

    [Fact]
    public async Task TransactionBehavior_Handle_Query_DelegatesToNext()
    {
        // Arrange
        var behavior = new TransactionBehavior<GetProductByIdQuery, ProductDto?>();
        var query = new GetProductByIdQuery(Guid.NewGuid());
        var expectedDto = new ProductDto { Id = Guid.NewGuid(), Name = "Test Product", SKU = "SKU-005" };

        RequestHandlerDelegate<ProductDto?> next = () => Task.FromResult<ProductDto?>(expectedDto);

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedDto);
    }

    [Fact]
    public async Task TransactionBehavior_Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new TransactionBehavior<CreateProductCommand, CreateProductResult>();

        // Act
        var act = () => sut.Handle(null!, () => Task.FromResult(new CreateProductResult()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    // ───────────────────────── ValidationBehavior ─────────────────────────

    [Fact]
    public async Task ValidationBehavior_Handle_NoValidators_ExecutesNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<CreateProductCommand>>();
        var behavior = new ValidationBehavior<CreateProductCommand, CreateProductResult>(validators);
        var command = new CreateProductCommand("Test", "SKU-007", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true };

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task ValidationBehavior_Handle_AllValidatorsPass_ExecutesNext()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<CreateProductCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<CreateProductCommand>> { mockValidator.Object };
        var behavior = new ValidationBehavior<CreateProductCommand, CreateProductResult>(validators);
        var command = new CreateProductCommand("Test", "SKU-008", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true };

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task ValidationBehavior_Handle_ValidatorFails_ThrowsValidationException()
    {
        // Arrange
        var failure = new ValidationFailure("Name", "Name is required");
        var validationResult = new ValidationResult(new List<ValidationFailure> { failure });

        var mockValidator = new Mock<IValidator<CreateProductCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<CreateProductCommand>> { mockValidator.Object };
        var behavior = new ValidationBehavior<CreateProductCommand, CreateProductResult>(validators);
        var command = new CreateProductCommand("", "SKU-009", null, 10m, 20m, Guid.NewGuid());

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(new CreateProductResult());

        // Act
        Func<Task> act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required");
    }

    [Fact]
    public async Task ValidationBehavior_Handle_MultipleValidators_AllExecuted()
    {
        // Arrange
        var mockValidator1 = new Mock<IValidator<CreateProductCommand>>();
        mockValidator1
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var mockValidator2 = new Mock<IValidator<CreateProductCommand>>();
        mockValidator2
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<CreateProductCommand>> { mockValidator1.Object, mockValidator2.Object };
        var behavior = new ValidationBehavior<CreateProductCommand, CreateProductResult>(validators);
        var command = new CreateProductCommand("Test", "SKU-010", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true };

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        mockValidator1.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockValidator2.Verify(
            v => v.ValidateAsync(It.IsAny<ValidationContext<CreateProductCommand>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ───────────────────────── TenantFilterBehavior ─────────────────────────

    [Fact]
    public async Task TenantFilterBehavior_Handle_ValidTenantId_ExecutesNext()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTenantProvider = new Mock<ITenantProvider>();
        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(tenantId);
        var mockLogger = new Mock<ILogger<TenantFilterBehavior<CreateProductCommand, CreateProductResult>>>();

        var behavior = new TenantFilterBehavior<CreateProductCommand, CreateProductResult>(
            mockTenantProvider.Object, mockLogger.Object);

        var command = new CreateProductCommand("Test", "SKU-011", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true, ProductId = Guid.NewGuid() };

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        mockTenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    [Fact]
    public async Task TenantFilterBehavior_Handle_EmptyTenantId_StillExecutesNext()
    {
        // Arrange
        var mockTenantProvider = new Mock<ITenantProvider>();
        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.Empty);
        var mockLogger = new Mock<ILogger<TenantFilterBehavior<CreateProductCommand, CreateProductResult>>>();

        var behavior = new TenantFilterBehavior<CreateProductCommand, CreateProductResult>(
            mockTenantProvider.Object, mockLogger.Object);

        var command = new CreateProductCommand("Test", "SKU-012", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true };
        var nextCalled = false;

        RequestHandlerDelegate<CreateProductResult> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        nextCalled.Should().BeTrue("TenantFilterBehavior should not block execution when TenantId is empty");
    }

    [Fact]
    public async Task TenantFilterBehavior_Handle_AlwaysPassesThrough_ReturnsResponse()
    {
        // Arrange
        var mockTenantProvider = new Mock<ITenantProvider>();
        mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        var mockLogger = new Mock<ILogger<TenantFilterBehavior<CreateProductCommand, CreateProductResult>>>();

        var behavior = new TenantFilterBehavior<CreateProductCommand, CreateProductResult>(
            mockTenantProvider.Object, mockLogger.Object);

        var command = new CreateProductCommand("Test", "SKU-013", null, 10m, 20m, Guid.NewGuid());
        var expectedResponse = new CreateProductResult { IsSuccess = true, ProductId = Guid.NewGuid() };

        RequestHandlerDelegate<CreateProductResult> next = () => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ProductId.Should().Be(expectedResponse.ProductId);
    }

    [Fact]
    public void TenantFilterBehavior_Handle_NullTenantProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TenantFilterBehavior<CreateProductCommand, CreateProductResult>>>();

        // Act
        Action act = () => new TenantFilterBehavior<CreateProductCommand, CreateProductResult>(null!, mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("tenantProvider");
    }
}
