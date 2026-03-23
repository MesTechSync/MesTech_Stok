using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetFixedExpensesHandler tests — sabit gider listesi, Mapster mapping ve bos liste senaryosu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetFixedExpensesHandlerTests
{
    private readonly Mock<IFixedExpenseRepository> _repoMock;
    private readonly GetFixedExpensesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetFixedExpensesHandlerTests()
    {
        _repoMock = new Mock<IFixedExpenseRepository>();
        _sut = new GetFixedExpensesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsMappedFixedExpenseList()
    {
        // Arrange
        var expenses = new List<FixedExpense>
        {
            FixedExpense.Create(_tenantId, "Ofis Kirasi", 12000m, 1, new DateTime(2025, 1, 1)),
            FixedExpense.Create(_tenantId, "Internet", 800m, 15, new DateTime(2025, 3, 1), "TRY", null, "Turkcell")
        };

        var query = new GetFixedExpensesQuery(_tenantId, IsActive: true);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(dto => dto.Name == "Ofis Kirasi" && dto.MonthlyAmount == 12000m);
        result.Should().Contain(dto => dto.Name == "Internet" && dto.DayOfMonth == 15);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetFixedExpensesQuery(_tenantId, IsActive: null);

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FixedExpense>().AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_InactiveFilter_PassesCorrectFlagToRepository()
    {
        // Arrange
        var query = new GetFixedExpensesQuery(_tenantId, IsActive: false);
        var deactivatedExpense = FixedExpense.Create(_tenantId, "Eski Sigorta", 2500m, 20, new DateTime(2024, 6, 1));
        deactivatedExpense.Deactivate();

        _repoMock
            .Setup(r => r.GetAllAsync(_tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FixedExpense> { deactivatedExpense }.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _repoMock.Verify(
            r => r.GetAllAsync(_tenantId, false, It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
