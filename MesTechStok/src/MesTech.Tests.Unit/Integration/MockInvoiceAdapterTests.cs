using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;

namespace MesTech.Tests.Unit.Integration;

public class MockInvoiceAdapterTests
{
    private readonly MockInvoiceAdapter _adapter;

    public MockInvoiceAdapterTests()
    {
        var provider = new MockInvoiceProvider();
        _adapter = new MockInvoiceAdapter(provider);
    }

    [Fact]
    public void ProviderName_Should_Be_Mock()
    {
        Assert.Equal("Mock e-Fatura (Test)", _adapter.ProviderName);
    }

    [Fact]
    public void ProviderType_Should_Be_GibEntegrator()
    {
        Assert.Equal(InvoiceProviderType.GibEntegrator, _adapter.ProviderType);
    }

    [Fact]
    public void Provider_Should_Return_Inner_Provider()
    {
        Assert.IsType<MockInvoiceProvider>(_adapter.Provider);
    }

    [Fact]
    public void Capability_Flags_All_False()
    {
        Assert.False(_adapter.SupportsBulkInvoice);
        Assert.False(_adapter.SupportsIncomingInvoice);
        Assert.False(_adapter.SupportsTemplateCustomization);
        Assert.False(_adapter.SupportsKontorBalance);
    }

    [Fact]
    public void Should_Not_Implement_Any_Capability()
    {
        Assert.IsNotType<IBulkInvoiceCapable>(_adapter);
        Assert.IsNotType<IIncomingInvoiceCapable>(_adapter);
        Assert.IsNotType<IKontorCapable>(_adapter);
        Assert.IsNotType<IInvoiceTemplateCapable>(_adapter);
    }

    [Fact]
    public async Task CreateInvoiceAsync_Delegates_To_Provider()
    {
        var request = new InvoiceCreateRequest
        {
            OrderId = Guid.NewGuid(),
            Type = InvoiceType.EFatura,
            Customer = new InvoiceCustomerInfo("Test", "1234567890", null, "Address", null, null),
            Lines = [],
            TotalAmount = 100m
        };

        var result = await _adapter.CreateInvoiceAsync(request);
        Assert.True(result.Success);
        Assert.StartsWith("GIB", result.GibInvoiceId);
    }

    [Fact]
    public async Task IsEFaturaMukellefAsync_Delegates_To_Provider()
    {
        Assert.True(await _adapter.IsEFaturaMukellefAsync("3456789012"));
        Assert.False(await _adapter.IsEFaturaMukellefAsync("1234567890"));
    }

    [Fact]
    public async Task GetInvoiceXmlAsync_Returns_NotSupported_Placeholder()
    {
        var xml = await _adapter.GetInvoiceXmlAsync("test-id");
        Assert.Contains("xml", xml, StringComparison.OrdinalIgnoreCase);
    }
}
