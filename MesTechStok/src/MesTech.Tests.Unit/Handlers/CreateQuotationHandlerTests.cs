using FluentAssertions;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateQuotationHandler _sut;

    public CreateQuotationHandlerTests()
    {
        _sut = new CreateQuotationHandler(_quotationRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesQuotation()
    {
        var cmd = new CreateQuotationCommand(
            QuotationNumber: "QUO-001",
            ValidUntil: DateTime.UtcNow.AddDays(30),
            CustomerName: "Test Müşteri");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.QuotationId.Should().NotBe(Guid.Empty);

        _quotationRepoMock.Verify(r => r.AddAsync(It.Is<Quotation>(q =>
            q.QuotationNumber == "QUO-001" && q.CustomerName == "Test Müşteri"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
