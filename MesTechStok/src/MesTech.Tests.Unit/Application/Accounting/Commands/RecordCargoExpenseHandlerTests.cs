using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class RecordCargoExpenseHandlerTests
{
    private readonly Mock<ICargoExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RecordCargoExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public RecordCargoExpenseHandlerTests()
    {
        _sut = new RecordCargoExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new RecordCargoExpenseCommand(
            TenantId, "Yurtici Kargo", 25.50m, "ORD-123", "YK123456");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutOptionalFields_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new RecordCargoExpenseCommand(TenantId, "Aras Kargo", 18.75m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
