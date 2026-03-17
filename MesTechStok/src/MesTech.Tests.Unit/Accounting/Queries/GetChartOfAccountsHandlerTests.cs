using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetChartOfAccountsHandler tests — hesap listesi sorgulama ve DTO mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetChartOfAccountsHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock;
    private readonly GetChartOfAccountsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetChartOfAccountsHandlerTests()
    {
        _repoMock = new Mock<IChartOfAccountsRepository>();
        _sut = new GetChartOfAccountsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsMappedDtoList()
    {
        // Arrange
        var accounts = new List<ChartOfAccounts>
        {
            ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset),
            ChartOfAccounts.Create(_tenantId, "320", "Saticilar", AccountType.Liability),
            ChartOfAccounts.Create(_tenantId, "600", "Yurt Ici Satislar", AccountType.Revenue)
        };

        var query = new GetChartOfAccountsQuery(_tenantId, IsActive: true);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(dto => dto.Code == "100" && dto.Name == "Kasa");
        result.Should().Contain(dto => dto.Code == "320" && dto.AccountType == AccountType.Liability.ToString());
        result.Should().Contain(dto => dto.Code == "600" && dto.IsActive);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetChartOfAccountsQuery(_tenantId, IsActive: null);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChartOfAccounts>().AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
