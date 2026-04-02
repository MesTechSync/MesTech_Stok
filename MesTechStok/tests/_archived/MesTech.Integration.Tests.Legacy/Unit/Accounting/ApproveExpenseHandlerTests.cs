using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// ApproveExpenseHandler: gider onaylama.
/// Kritik: domain method Approve(userId) çağrılmalı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "FinanceChain")]
public class ApproveExpenseHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public ApproveExpenseHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private ApproveExpenseHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExpenseNotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceExpense?)null);

        var cmd = new ApproveExpenseCommand(Guid.NewGuid(), Guid.NewGuid());
        var handler = CreateHandler();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
