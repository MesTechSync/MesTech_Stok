using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateFixedExpenseHandlerTests
{
    private readonly Mock<IFixedExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateFixedExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateFixedExpenseHandlerTests()
    {
        _sut = new CreateFixedExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateFixedExpenseCommand(
            TenantId, "Internet", 500m, 15, DateTime.Today);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<FixedExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEndDateAndSupplier_ShouldCreateSuccessfully()
    {
        // Arrange
        var supplierId = Guid.NewGuid();
        var command = new CreateFixedExpenseCommand(
            TenantId, "Depo Kirasi", 10000m, 1, DateTime.Today,
            EndDate: DateTime.Today.AddYears(1), SupplierName: "ABC Ltd", SupplierId: supplierId);

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
