using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Invoice.Queries;

[Trait("Category", "Unit")]
public class GetInvoiceProvidersHandlerTests
{
    private GetInvoiceProvidersHandler CreateHandler() => new();

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturn9Providers()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoiceProvidersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(9);
        result.Select(p => p.Provider).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_ShouldIncludeActiveSovosProvider()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoiceProvidersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var sovos = result.FirstOrDefault(p => p.Provider == InvoiceProvider.Sovos);
        sovos.Should().NotBeNull();
        sovos!.IsConfigured.Should().BeTrue();
        sovos.IsActive.Should().BeTrue();
        sovos.IsReal.Should().BeTrue();
        sovos.SupportedTypes.Should().Contain("EIhracat");
    }

    [Fact]
    public async Task Handle_ManualProvider_ShouldBeActiveButNotReal()
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetInvoiceProvidersQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var manual = result.FirstOrDefault(p => p.Provider == InvoiceProvider.Manual);
        manual.Should().NotBeNull();
        manual!.IsActive.Should().BeTrue();
        manual.IsReal.Should().BeFalse();
        manual.IsConfigured.Should().BeTrue();
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
