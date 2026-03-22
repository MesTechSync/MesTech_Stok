using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeleteChartOfAccountHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteChartOfAccountHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeleteChartOfAccountHandlerTests()
    {
        _sut = new DeleteChartOfAccountHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingAccount_ShouldMarkDeletedAndReturnTrue()
    {
        // Arrange
        var account = ChartOfAccounts.Create(TenantId, "100", "Kasa", AccountType.Asset);
        _repoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        var command = new DeleteChartOfAccountCommand(account.Id, "admin");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentAccount_ShouldReturnFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        var command = new DeleteChartOfAccountCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
