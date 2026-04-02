using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Invoice.Queries;

[Trait("Category", "Unit")]
public class GetInvoiceReportHandlerTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetInvoiceReportHandlerTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        _repository.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<global::MesTech.Domain.Entities.Invoice>());
    }

    private GetInvoiceReportHandler CreateHandler() =>
        new(_repository.Object, _tenantProvider.Object);

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnReportWithZeroTotals()
    {
        // Arrange — stub handler returns zeroed report
        var handler = CreateHandler();
        var query = new GetInvoiceReportQuery(
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow,
            Platform: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.TotalAmount.Should().Be(0m);
        result.ByPlatform.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPlatformFilter_ShouldStillReturnReport()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoiceReportQuery(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            MesTech.Domain.Enums.PlatformType.Trendyol);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EFaturaCount.Should().Be(0);
        result.EArsivCount.Should().Be(0);
        result.EIhracatCount.Should().Be(0);
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
