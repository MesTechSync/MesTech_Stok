using FluentAssertions;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateIncomeHandler testi — gelir güncelleme.
/// P1 iş-kritik: muhasebe bütünlüğü.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateIncomeHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateIncomeHandler CreateSut() => new(_incomeRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_IncomeNotFound_ShouldThrowKeyNotFound()
    {
        _incomeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Income?)null);
        var cmd = new UpdateIncomeCommand(Guid.NewGuid(), Description: "test");

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_UpdateDescription_ShouldChangeOnly()
    {
        var income = new Income { Description = "Old", TenantId = Guid.NewGuid() };
        income.SetAmount(1000m);
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id)).ReturnsAsync(income);

        var cmd = new UpdateIncomeCommand(income.Id, Description: "Updated Income");
        await CreateSut().Handle(cmd, CancellationToken.None);

        income.Description.Should().Be("Updated Income");
        income.Amount.Should().Be(1000m);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateAmount_ShouldCallSetAmount()
    {
        var income = new Income { Description = "Satış", TenantId = Guid.NewGuid() };
        income.SetAmount(500m);
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id)).ReturnsAsync(income);

        var cmd = new UpdateIncomeCommand(income.Id, Amount: 750m);
        await CreateSut().Handle(cmd, CancellationToken.None);

        income.Amount.Should().Be(750m);
    }

    [Fact]
    public async Task Handle_UpdateIncomeType_ShouldChange()
    {
        var income = new Income { Description = "Gelir", TenantId = Guid.NewGuid(), IncomeType = IncomeType.Satis };
        income.SetAmount(100m);
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id)).ReturnsAsync(income);

        var cmd = new UpdateIncomeCommand(income.Id, IncomeType: IncomeType.Hizmet);
        await CreateSut().Handle(cmd, CancellationToken.None);

        income.IncomeType.Should().Be(IncomeType.Hizmet);
    }

    [Fact]
    public async Task Handle_AllNull_ShouldNotChangeAnything()
    {
        var income = new Income { Description = "Original", TenantId = Guid.NewGuid(), IncomeType = IncomeType.Satis };
        income.SetAmount(200m);
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id)).ReturnsAsync(income);

        var cmd = new UpdateIncomeCommand(income.Id);
        await CreateSut().Handle(cmd, CancellationToken.None);

        income.Description.Should().Be("Original");
        income.Amount.Should().Be(200m);
    }
}
