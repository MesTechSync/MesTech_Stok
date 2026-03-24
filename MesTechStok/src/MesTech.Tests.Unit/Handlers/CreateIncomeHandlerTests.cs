using FluentAssertions;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateIncomeHandlerTests
{
    private readonly Mock<IIncomeRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateIncomeHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateIncomeHandlerTests()
    {
        _repo = new Mock<IIncomeRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateIncomeHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidIncome_ReturnsGuidAndCallsAdd()
    {
        var command = new CreateIncomeCommand(
            _tenantId, null, "Satis geliri", 500m, IncomeType.Satis, null, DateTime.UtcNow, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.TenantId == _tenantId && i.Description == "Satis geliri")), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithInvoiceId_SetsInvoiceReference()
    {
        var invoiceId = Guid.NewGuid();
        var command = new CreateIncomeCommand(
            _tenantId, null, "Fatura geliri", 1000m, IncomeType.Satis,
            invoiceId, DateTime.UtcNow, "fatura notu");

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.InvoiceId == invoiceId)), Times.Once());
    }

    [Fact]
    public async Task Handle_NullDate_DefaultsToUtcNow()
    {
        var command = new CreateIncomeCommand(
            _tenantId, null, "Test", 10m, IncomeType.Diger, null, null, null);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.Date.Date == DateTime.UtcNow.Date)), Times.Once());
    }
}
