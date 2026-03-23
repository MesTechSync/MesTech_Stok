using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class BulkCreateInvoiceValidatorTests
{
    private readonly BulkCreateInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new BulkCreateInvoiceCommand(
            new List<Guid> { Guid.NewGuid() },
            InvoiceProvider.Sovos
        );
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyOrderIds_ShouldFail()
    {
        var cmd = new BulkCreateInvoiceCommand(new List<Guid>(), InvoiceProvider.Sovos);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderIds");
    }

    [Fact]
    public async Task MoreThan100Orders_ShouldFail()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkCreateInvoiceCommand(ids, InvoiceProvider.Sovos);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderIds");
    }

    [Fact]
    public async Task Exactly100Orders_ShouldPass()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var cmd = new BulkCreateInvoiceCommand(ids, InvoiceProvider.Sovos);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidProvider_ShouldFail()
    {
        var cmd = new BulkCreateInvoiceCommand(
            new List<Guid> { Guid.NewGuid() },
            (InvoiceProvider)99
        );
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }
}
