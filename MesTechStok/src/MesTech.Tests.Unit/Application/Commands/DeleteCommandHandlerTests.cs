using FluentAssertions;
using MesTech.Application.Commands.DeleteCariHareket;
using MesTech.Application.Commands.DeleteCariHesap;
using MesTech.Application.Commands.DeleteCustomer;
using MesTech.Application.Commands.DeleteQuotation;
using MesTech.Application.Commands.DeleteStockLot;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Commands;

[Trait("Category", "Unit")]
[Trait("Feature", "DeleteCommands")]
public class DeleteCommandHandlerTests
{
    // ─── DeleteCariHareket ──────────────────────────────────────

    [Fact]
    public async Task DeleteCariHareket_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<ICariHareketRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = CariHareket.Create(
            Guid.NewGuid(), Guid.NewGuid(), 100m,
            CariDirection.Borc, "Test hareket");

        repo.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteCariHareketHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCariHareketCommand(entity.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCariHareket_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<ICariHareketRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CariHareket?)null);

        var handler = new DeleteCariHareketHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCariHareketCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteCariHesap ────────────────────────────────────────

    [Fact]
    public async Task DeleteCariHesap_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<ICariHesapRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = CariHesap.Create(
            Guid.NewGuid(), "Test Hesap", CariHesapType.Musteri);

        repo.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteCariHesapHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCariHesapCommand(entity.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        repo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCariHesap_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<ICariHesapRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CariHesap?)null);

        var handler = new DeleteCariHesapHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCariHesapCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        repo.Verify(r => r.UpdateAsync(It.IsAny<CariHesap>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteCustomer ─────────────────────────────────────────

    [Fact]
    public async Task DeleteCustomer_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<ICustomerRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = Customer.Create(
            Guid.NewGuid(), "Test Customer", "CUST-001",
            "test@example.com", "555-0100");

        repo.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteCustomerHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCustomerCommand(entity.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        repo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteCustomer_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<ICustomerRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var handler = new DeleteCustomerHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteCustomerCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        repo.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteQuotation ────────────────────────────────────────

    [Fact]
    public async Task DeleteQuotation_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<IQuotationRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = new Quotation
        {
            TenantId = Guid.NewGuid(),
            QuotationNumber = "QT-001",
            CustomerName = "Test Customer"
        };

        repo.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteQuotationHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteQuotationCommand(entity.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        repo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteQuotation_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<IQuotationRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quotation?)null);

        var handler = new DeleteQuotationHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteQuotationCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        repo.Verify(r => r.UpdateAsync(It.IsAny<Quotation>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── DeleteStockLot ─────────────────────────────────────────

    [Fact]
    public async Task DeleteStockLot_ExistingEntity_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var repo = new Mock<IStockLotRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = StockLot.Create(
            Guid.NewGuid(), Guid.NewGuid(), "LOT-001",
            quantity: 10, unitCost: 50m);

        repo.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var handler = new DeleteStockLotHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteStockLotCommand(entity.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteStockLot_NonExistentEntity_ShouldReturnError()
    {
        // Arrange
        var repo = new Mock<IStockLotRepository>();
        var uow = new Mock<IUnitOfWork>();
        var missingId = Guid.NewGuid();

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockLot?)null);

        var handler = new DeleteStockLotHandler(repo.Object, uow.Object);

        // Act
        var result = await handler.Handle(
            new DeleteStockLotCommand(missingId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
