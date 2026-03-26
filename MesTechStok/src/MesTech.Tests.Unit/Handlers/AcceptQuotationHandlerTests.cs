using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class AcceptQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly AcceptQuotationHandler _sut;

    public AcceptQuotationHandlerTests()
    {
        _sut = new AcceptQuotationHandler(_quotationRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentQuotation_ReturnsFail()
    {
        var quotationId = Guid.NewGuid();
        _quotationRepoMock.Setup(r => r.GetByIdAsync(quotationId)).ReturnsAsync((Quotation?)null);

        var cmd = new AcceptQuotationCommand(quotationId);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
