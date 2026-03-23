using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetCounterpartiesHandler tests — cari listesi, filtre ve DTO mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCounterpartiesHandlerTests
{
    private readonly Mock<ICounterpartyRepository> _repoMock;
    private readonly GetCounterpartiesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetCounterpartiesHandlerTests()
    {
        _repoMock = new Mock<ICounterpartyRepository>();
        _sut = new GetCounterpartiesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsMappedCounterpartyList()
    {
        // Arrange
        var counterparties = new List<Counterparty>
        {
            Counterparty.Create(_tenantId, "Trendyol", CounterpartyType.Platform, platform: "Trendyol"),
            Counterparty.Create(_tenantId, "ABC Tedarik", CounterpartyType.Supplier, vkn: "1234567890", phone: "05551234567"),
            Counterparty.Create(_tenantId, "Garanti Bankasi", CounterpartyType.Bank)
        };

        var query = new GetCounterpartiesQuery(_tenantId);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterparties.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(dto => dto.Name == "Trendyol" && dto.CounterpartyType == CounterpartyType.Platform.ToString());
        result.Should().Contain(dto => dto.Name == "ABC Tedarik" && dto.VKN == "1234567890");
        result.Should().Contain(dto => dto.Name == "Garanti Bankasi" && dto.CounterpartyType == CounterpartyType.Bank.ToString());
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetCounterpartiesQuery(_tenantId, CounterpartyType.Carrier);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, CounterpartyType.Carrier, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Counterparty>().AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTypeFilter_PassesTypeToRepository()
    {
        // Arrange
        var query = new GetCounterpartiesQuery(_tenantId, CounterpartyType.Supplier, IsActive: false);

        var suppliers = new List<Counterparty>
        {
            Counterparty.Create(_tenantId, "Deaktif Tedarikci", CounterpartyType.Supplier)
        };

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, CounterpartyType.Supplier, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppliers.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].CounterpartyType.Should().Be(CounterpartyType.Supplier.ToString());
        _repoMock.Verify(
            r => r.GetAllAsync(_tenantId, CounterpartyType.Supplier, false, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
