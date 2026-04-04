using FluentAssertions;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Returns;

[Trait("Category", "Unit")]
[Trait("Domain", "Returns")]
public class RejectReturnHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returnRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RejectReturnHandler CreateSut() => new(_returnRepo.Object, _uow.Object);

    private static ReturnRequest CreatePendingReturn(Guid returnId)
    {
        var rr = ReturnRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlatformType.Trendyol,
            ReturnReason.WrongProduct,
            "Test Customer");
        typeof(ReturnRequest).GetProperty("Id")!.SetValue(rr, returnId);
        return rr;
    }

    [Fact]
    public async Task Handle_ValidPendingReturn_RejectsSuccessfully()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var returnRequest = CreatePendingReturn(returnId);
        var command = new RejectReturnCommand(returnId, "Urun kullanilmis");

        _returnRepo.Setup(r => r.GetByIdAsync(returnId, It.IsAny<CancellationToken>())).ReturnsAsync(returnRequest);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _returnRepo.Verify(r => r.UpdateAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnNotFound_ReturnsFailure()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var command = new RejectReturnCommand(returnId, "Test reason");

        _returnRepo.Setup(r => r.GetByIdAsync(returnId, It.IsAny<CancellationToken>())).ReturnsAsync((ReturnRequest?)null);

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
    public async Task Handle_AlreadyRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        var returnId = Guid.NewGuid();
        var returnRequest = CreatePendingReturn(returnId);
        returnRequest.Reject("Already rejected"); // Now status is Rejected

        var command = new RejectReturnCommand(returnId, "Another rejection");
        _returnRepo.Setup(r => r.GetByIdAsync(returnId, It.IsAny<CancellationToken>())).ReturnsAsync(returnRequest);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bekleyen*");
    }
}
