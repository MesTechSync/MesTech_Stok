using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetInvoiceSettingsHandlerTests
{
    private readonly GetInvoiceSettingsHandler _sut = new();

    [Fact]
    public async Task Handle_ReturnsDefaultSettings()
    {
        var query = new GetInvoiceSettingsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.DefaultProvider.Should().Be("None");
        result.DefaultScenario.Should().Be("Basic");
        result.DefaultCurrency.Should().Be("TRY");
        result.DefaultTaxRate.Should().Be(0.20m);
        result.InvoicePrefix.Should().Be("INV");
        result.NextInvoiceNumber.Should().Be(1);
        result.AutoApprove.Should().BeFalse();
        result.AutoSendToGib.Should().BeFalse();
    }
}
