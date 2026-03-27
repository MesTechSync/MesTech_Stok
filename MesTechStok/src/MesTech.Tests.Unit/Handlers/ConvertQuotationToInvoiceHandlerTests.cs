using FluentAssertions;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ConvertQuotationToInvoiceHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepoMock = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ConvertQuotationToInvoiceHandler _sut;

    public ConvertQuotationToInvoiceHandlerTests()
    {
        _sut = new ConvertQuotationToInvoiceHandler(
            _quotationRepoMock.Object, _invoiceRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentQuotation_ReturnsFail()
    {
        var quotationId = Guid.NewGuid();
        _quotationRepoMock.Setup(r => r.GetByIdWithLinesAsync(quotationId)).ReturnsAsync((Quotation?)null);

        var cmd = new ConvertQuotationToInvoiceCommand(quotationId, "INV-001");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        _invoiceRepoMock.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
