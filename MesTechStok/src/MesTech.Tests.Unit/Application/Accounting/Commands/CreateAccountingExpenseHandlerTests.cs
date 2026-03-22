using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateAccountingExpenseHandlerTests
{
    private readonly Mock<IPersonalExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateAccountingExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateAccountingExpenseHandlerTests()
    {
        _sut = new CreateAccountingExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateExpenseAndReturnId()
    {
        // Arrange
        var command = new CreateAccountingExpenseCommand(
            TenantId, "Ofis Kirasi", 5000m, DateTime.Today, ExpenseSource.Manual, "Kira");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PersonalExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhatsAppSource_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateAccountingExpenseCommand(
            TenantId, "Yemek", 150m, DateTime.Today, ExpenseSource.WhatsApp);

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
