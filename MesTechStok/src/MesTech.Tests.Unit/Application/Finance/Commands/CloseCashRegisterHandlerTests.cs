using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance.Commands;

[Trait("Category", "Unit")]
public class CloseCashRegisterHandlerTests
{
    private readonly Mock<ICashRegisterRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CloseCashRegisterHandler>> _loggerMock = new();
    private readonly CloseCashRegisterHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CloseCashRegisterHandlerTests()
    {
        _sut = new CloseCashRegisterHandler(_repoMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExactBalance_ShouldCloseWithZeroDifference()
    {
        // Arrange
        var cashRegister = CashRegister.Create(TenantId, "Ana Kasa", "TRY", true, 5000m);
        _repoMock.Setup(r => r.GetByIdAsync(cashRegister.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashRegister);
        var command = new CloseCashRegisterCommand(TenantId, cashRegister.Id, DateTime.Today, 5000m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsClosed.Should().BeTrue();
        result.CashDifference.Should().Be(0m);
        result.ExpectedBalance.Should().Be(5000m);
        result.ActualBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task Handle_NonExistentRegister_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashRegister?)null);
        var command = new CloseCashRegisterCommand(TenantId, Guid.NewGuid(), DateTime.Today, 1000m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bulunamadi*");
    }

    [Fact]
    public async Task Handle_DifferentTenant_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cashRegister = CashRegister.Create(TenantId, "Kasa", "TRY", true, 1000m);
        _repoMock.Setup(r => r.GetByIdAsync(cashRegister.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashRegister);
        var otherTenant = Guid.NewGuid();
        var command = new CloseCashRegisterCommand(otherTenant, cashRegister.Id, DateTime.Today, 1000m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tenant*");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
