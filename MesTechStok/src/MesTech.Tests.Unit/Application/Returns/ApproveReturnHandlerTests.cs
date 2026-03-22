using FluentAssertions;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Returns;

[Trait("Category", "Unit")]
[Trait("Domain", "Returns")]
public class ApproveReturnHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returnRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ApproveReturnHandler CreateSut() =>
        new(_returnRepo.Object, _productRepo.Object, _uow.Object);

    private static ReturnRequest CreatePendingReturn(Guid returnId)
    {
        var rr = ReturnRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlatformType.Trendyol,
            ReturnReason.DefectiveProduct,
            "Test Customer",
            "Urun bozuk");
        typeof(ReturnRequest).GetProperty("Id")!.SetValue(rr, returnId);
        return rr;
    }

    [Fact]
    public async Task Handle_ValidPendingReturn_ApprovesSuccessfully()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var returnRequest = CreatePendingReturn(returnId);
        var command = new ApproveReturnCommand(returnId, AutoRestoreStock: false);

        _returnRepo.Setup(r => r.GetByIdAsync(returnId)).ReturnsAsync(returnRequest);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StockRestored.Should().BeFalse();
        _returnRepo.Verify(r => r.UpdateAsync(It.IsAny<ReturnRequest>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnNotFound_ReturnsFailure()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var command = new ApproveReturnCommand(returnId);

        _returnRepo.Setup(r => r.GetByIdAsync(returnId)).ReturnsAsync((ReturnRequest?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var returnRequest = CreatePendingReturn(returnId);
        returnRequest.Approve(); // Now status is Approved

        var command = new ApproveReturnCommand(returnId);
        _returnRepo.Setup(r => r.GetByIdAsync(returnId)).ReturnsAsync(returnRequest);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bekleyen*");
    }
}
