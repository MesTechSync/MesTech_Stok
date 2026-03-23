using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Queries;

[Trait("Category", "Unit")]
public class GetBillingInvoicesHandlerTests
{
    private readonly Mock<IBillingInvoiceRepository> _repoMock = new();
    private readonly GetBillingInvoicesHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public GetBillingInvoicesHandlerTests()
    {
        _sut = new GetBillingInvoicesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_TenantHasInvoices_ShouldReturnMappedDtos()
    {
        // Arrange
        var invoice = BillingInvoice.Create(TenantId, Guid.NewGuid(), "MEST-2026-000001", 799m);
        _repoMock.Setup(r => r.GetByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingInvoice> { invoice }.AsReadOnly());
        var query = new GetBillingInvoicesQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].InvoiceNumber.Should().Be("MEST-2026-000001");
        result[0].Amount.Should().Be(799m);
        result[0].Status.Should().Be(BillingInvoiceStatus.Draft);
    }

    [Fact]
    public async Task Handle_TenantHasNoInvoices_ShouldReturnEmptyList()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingInvoice>().AsReadOnly());
        var query = new GetBillingInvoicesQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_MultipleInvoices_ShouldReturnAll()
    {
        // Arrange
        var inv1 = BillingInvoice.Create(TenantId, Guid.NewGuid(), "MEST-2026-000001", 100m);
        var inv2 = BillingInvoice.Create(TenantId, Guid.NewGuid(), "MEST-2026-000002", 200m);
        var inv3 = BillingInvoice.Create(TenantId, Guid.NewGuid(), "MEST-2026-000003", 300m);
        _repoMock.Setup(r => r.GetByTenantIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BillingInvoice> { inv1, inv2, inv3 }.AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetBillingInvoicesQuery(TenantId), CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(x => x.Amount).Should().BeEquivalentTo(new[] { 100m, 200m, 300m });
    }
}
