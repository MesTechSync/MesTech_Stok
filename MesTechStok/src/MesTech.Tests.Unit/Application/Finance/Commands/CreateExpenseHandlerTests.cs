using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance.Commands;

[Trait("Category", "Unit")]
public class CreateExpenseHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateExpenseHandlerTests()
    {
        _sut = new CreateExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateExpenseCommand(
            TenantId, "Yazilim Lisansi", 2500m, ExpenseCategory.Software, DateTime.Today);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<FinanceExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAllOptionalFields_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var command = new CreateExpenseCommand(
            TenantId, "Ofis Malzemesi", 350m, ExpenseCategory.Other,
            DateTime.Today, SubmittedByUserId: userId, Notes: "Kalem + Kagit", StoreId: storeId);

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
