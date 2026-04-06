using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5 Batch 7: Command handler testleri — MarkExpensePaid, UpdateTenant, VerifyTotp.
/// </summary>

#region MarkExpensePaid

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class MarkExpensePaidHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task Handle_ExpenseNotFound_ShouldThrow()
    {
        _expenseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceExpense?)null);

        var handler = new MarkExpensePaidHandler(_expenseRepo.Object, _uow.Object);
        var act = () => handler.Handle(new MarkExpensePaidCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_ApprovedExpense_ShouldMarkPaidAndSave()
    {
        var expense = FinanceExpense.Create(
            Guid.NewGuid(), "Test Expense", 500m, ExpenseCategory.Software,
            DateTime.UtcNow, Guid.NewGuid());
        expense.Submit();
        expense.Approve(Guid.NewGuid()); // must be submitted→approved before paid

        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var handler = new MarkExpensePaidHandler(_expenseRepo.Object, _uow.Object);
        await handler.Handle(new MarkExpensePaidCommand(expense.Id, Guid.NewGuid()), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Paid);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region UpdateTenant

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateTenantHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task Handle_TenantNotFound_ShouldReturnFalse()
    {
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var handler = new UpdateTenantHandler(_tenantRepo.Object, _uow.Object);
        var result = await handler.Handle(
            new UpdateTenantCommand(Guid.NewGuid(), "Test", null, true), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndReturnTrue()
    {
        var tenant = new MesTech.Domain.Entities.Tenant { Name = "Old", IsActive = false };
        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new UpdateTenantHandler(_tenantRepo.Object, _uow.Object);
        var result = await handler.Handle(
            new UpdateTenantCommand(tenant.Id, "Yeni İsim", "1234567890", true), CancellationToken.None);

        result.Should().BeTrue();
        tenant.Name.Should().Be("Yeni İsim");
        tenant.TaxNumber.Should().Be("1234567890");
        tenant.IsActive.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region VerifyTotp

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class VerifyTotpHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITotpService> _totpService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<VerifyTotpHandler>> _logger = new();

    private VerifyTotpHandler CreateSut() =>
        new(_userRepo.Object, _totpService.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnError()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateSut().Handle(
            new VerifyTotpCommand(Guid.NewGuid(), "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task Handle_NoTotpSecret_ShouldReturnError()
    {
        var user = new User { Username = "test", TotpSecret = null };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateSut().Handle(
            new VerifyTotpCommand(user.Id, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("MFA");
    }
}

#endregion
