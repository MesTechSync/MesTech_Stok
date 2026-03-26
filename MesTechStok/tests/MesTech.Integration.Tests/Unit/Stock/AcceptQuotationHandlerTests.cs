using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// AcceptQuotationHandler: teklif kabul — durum geçişi testi.
/// Kritik iş kuralları:
///   - Sadece Pending/Draft teklif kabul edilebilir
///   - Accepted durumda tekrar kabul etmek InvalidOperation
///   - Teklif bulunamazsa hata dönmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "QuotationChain")]
public class AcceptQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public AcceptQuotationHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _quotationRepo.Setup(r => r.UpdateAsync(It.IsAny<Quotation>())).Returns(Task.CompletedTask);
    }

    private AcceptQuotationHandler CreateHandler() =>
        new(_quotationRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_PendingQuotation_AcceptsSuccessfully()
    {
        var quotation = new Quotation
        {
            TenantId = Guid.NewGuid(),
            QuotationNumber = "TEK-001",
            CustomerName = "Müşteri A",
            Currency = "TRY"
        };
        quotation.Send(); // Draft → Sent (Accept sadece Sent'ten çalışır)
        _quotationRepo.Setup(r => r.GetByIdAsync(quotation.Id)).ReturnsAsync(quotation);

        var cmd = new AcceptQuotationCommand(quotation.Id);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuotationNotFound_ReturnsFailure()
    {
        _quotationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Quotation?)null);

        var cmd = new AcceptQuotationCommand(Guid.NewGuid());
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AlreadyAccepted_ReturnsFailure()
    {
        var quotation = new Quotation
        {
            TenantId = Guid.NewGuid(),
            QuotationNumber = "TEK-002",
            CustomerName = "Müşteri B",
            Currency = "TRY"
        };
        quotation.Send(); // Draft → Sent
        quotation.Accept(); // Sent → Accepted (şimdi zaten Accepted)
        _quotationRepo.Setup(r => r.GetByIdAsync(quotation.Id)).ReturnsAsync(quotation);

        var cmd = new AcceptQuotationCommand(quotation.Id);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
