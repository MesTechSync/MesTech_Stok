using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreateChartOfAccountHandler tests — account creation and duplicate prevention.
/// </summary>
[Trait("Category", "Unit")]
public class CreateChartOfAccountHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreateChartOfAccountHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateChartOfAccountHandlerTests()
    {
        _repoMock = new Mock<IChartOfAccountsRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreateChartOfAccountHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndCallsAddAsync()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByCodeAsync(_tenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        var command = new CreateChartOfAccountCommand(
            _tenantId, "100", "Kasa", AccountType.Asset);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<ChartOfAccounts>(a =>
                a.Code == "100" && a.Name == "Kasa" && a.AccountType == AccountType.Asset),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingAccount = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        _repoMock.Setup(r => r.GetByCodeAsync(_tenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        var command = new CreateChartOfAccountCommand(
            _tenantId, "100", "Kasa Duplicate", AccountType.Asset);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_WithParentId_CreatesAccountWithParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByCodeAsync(_tenantId, "100.01", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        var command = new CreateChartOfAccountCommand(
            _tenantId, "100.01", "Kasa TL", AccountType.Asset, parentId, 2);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<ChartOfAccounts>(a => a.ParentId == parentId && a.Level == 2),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
