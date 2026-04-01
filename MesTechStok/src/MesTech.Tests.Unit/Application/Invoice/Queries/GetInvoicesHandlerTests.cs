using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Invoice.Queries;

[Trait("Category", "Unit")]
public class GetInvoicesHandlerTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetInvoicesHandlerTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        _repository.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Invoice>());
    }

    private GetInvoicesHandler CreateHandler() =>
        new(_repository.Object, _tenantProvider.Object);

    [Fact]
    public async Task Handle_DefaultQuery_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoicesQuery(
            Type: null, Status: null, Platform: null,
            From: null, To: null, Search: null,
            Page: 1, PageSize: 50);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Handle_CustomPageSize_ShouldPreserveInResult()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoicesQuery(
            Type: null, Status: null, Platform: null,
            From: null, To: null, Search: null,
            Page: 3, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.HasPrevious.Should().BeTrue(); // Page=3 > 1
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
