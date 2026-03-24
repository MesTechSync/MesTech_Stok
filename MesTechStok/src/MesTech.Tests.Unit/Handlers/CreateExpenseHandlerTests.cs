using FluentAssertions;
using MesTech.Application.Commands.CreateExpense;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateExpenseHandlerTests
{
    private readonly Mock<IExpenseRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateExpenseHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateExpenseHandlerTests()
    {
        _repo = new Mock<IExpenseRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateExpenseHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidExpense_ReturnsGuidAndCallsAdd()
    {
        var command = new CreateExpenseCommand(
            _tenantId, null, "Kargo masrafi", 150m, ExpenseType.Kargo, DateTime.UtcNow, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<Expense>(e =>
            e.TenantId == _tenantId && e.Description == "Kargo masrafi")), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_RecurringExpense_SetsRecurrenceFields()
    {
        var command = new CreateExpenseCommand(
            _tenantId, null, "Kira", 5000m, ExpenseType.Kira,
            DateTime.UtcNow, null, IsRecurring: true, RecurrencePeriod: "Monthly");

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<Expense>(e =>
            e.IsRecurring == true && e.RecurrencePeriod == "Monthly")), Times.Once());
    }

    [Fact]
    public async Task Handle_NullDate_DefaultsToUtcNow()
    {
        var command = new CreateExpenseCommand(
            _tenantId, null, "Test", 10m, ExpenseType.Diger, null, null);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<Expense>(e =>
            e.Date.Date == DateTime.UtcNow.Date)), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
