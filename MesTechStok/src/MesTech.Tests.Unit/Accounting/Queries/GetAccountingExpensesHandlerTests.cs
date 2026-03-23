using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetAccountingExpensesHandler tests — gider listesi sorgulama, filtreleme ve DTO mapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetAccountingExpensesHandlerTests
{
    private readonly Mock<IPersonalExpenseRepository> _repoMock;
    private readonly GetAccountingExpensesHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetAccountingExpensesHandlerTests()
    {
        _repoMock = new Mock<IPersonalExpenseRepository>();
        _sut = new GetAccountingExpensesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsMappedExpenseList()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        var expenses = new List<PersonalExpense>
        {
            PersonalExpense.Create(_tenantId, "Ofis Kira", 5000m, new DateTime(2026, 1, 5), ExpenseSource.Manual, "Kira"),
            PersonalExpense.Create(_tenantId, "Internet Fatura", 800m, new DateTime(2026, 1, 10), ExpenseSource.Email, "Fatura")
        };

        var query = new GetAccountingExpensesQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Ofis Kira");
        result[0].Amount.Should().Be(5000m);
        result[0].Category.Should().Be("Kira");
        result[0].Source.Should().Be(ExpenseSource.Manual.ToString());
        result[1].Title.Should().Be("Internet Fatura");
        result[1].Source.Should().Be(ExpenseSource.Email.ToString());
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31);
        var query = new GetAccountingExpensesQuery(_tenantId, from, to);

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalExpense>().AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSourceFilter_PassesSourceToRepository()
    {
        // Arrange
        var from = new DateTime(2026, 2, 1);
        var to = new DateTime(2026, 2, 28);
        var source = ExpenseSource.WhatsApp;
        var query = new GetAccountingExpensesQuery(_tenantId, from, to, source);

        var expenses = new List<PersonalExpense>
        {
            PersonalExpense.Create(_tenantId, "WhatsApp Gider", 150m, new DateTime(2026, 2, 15), ExpenseSource.WhatsApp, "Diger")
        };

        _repoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, source, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses.AsReadOnly());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Source.Should().Be(ExpenseSource.WhatsApp.ToString());
        _repoMock.Verify(
            r => r.GetByDateRangeAsync(_tenantId, from, to, source, It.IsAny<CancellationToken>()),
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
