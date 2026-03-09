using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Integration;

public class InvoiceAdapterFactoryTests2
{
    [Fact]
    public void Resolve_Mock_Returns_MockAdapter()
    {
        var mockProvider = new MockInvoiceProvider();
        var mockAdapter = new MockInvoiceAdapter(mockProvider);
        var factory = new InvoiceAdapterFactory(
            new IInvoiceAdapter[] { mockAdapter },
            new Mock<ILogger<InvoiceAdapterFactory>>().Object);

        var resolved = factory.Resolve(InvoiceProvider.Manual);
        Assert.NotNull(resolved);
        Assert.IsType<MockInvoiceAdapter>(resolved);
    }

    [Fact]
    public void Resolve_Unknown_Returns_Null()
    {
        var factory = new InvoiceAdapterFactory(
            Array.Empty<IInvoiceAdapter>(),
            new Mock<ILogger<InvoiceAdapterFactory>>().Object);

        Assert.Null(factory.Resolve(InvoiceProvider.Sovos));
    }

    [Fact]
    public void GetAll_Returns_All_Registered()
    {
        var mockProvider = new MockInvoiceProvider();
        var mockAdapter = new MockInvoiceAdapter(mockProvider);
        var factory = new InvoiceAdapterFactory(
            new IInvoiceAdapter[] { mockAdapter },
            new Mock<ILogger<InvoiceAdapterFactory>>().Object);

        var all = factory.GetAll();
        Assert.Single(all);
    }
}
