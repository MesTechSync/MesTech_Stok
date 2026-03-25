using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for quotation handlers — Create and Accept.
/// </summary>
[Trait("Category", "Unit")]
public class QuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ═══════ CreateQuotationHandler ═══════

    [Fact]
    public async Task CreateQuotation_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new CreateQuotationHandler(_repo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ AcceptQuotationHandler ═══════

    [Fact]
    public async Task AcceptQuotation_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new AcceptQuotationHandler(_repo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
