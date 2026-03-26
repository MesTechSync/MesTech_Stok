using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G050 devam: MediatR-bağımlı ViewModel batch 2 — 10 daha.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
[Trait("Group", "AvaloniaVMBatch2")]
public class AvaloniaViewModelBatch2Tests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public void ProductsVM_CanBeInstantiated()
    {
        var vm = new ProductsAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void InvoiceListVM_CanBeInstantiated()
    {
        var vm = new InvoiceListAvaloniaViewModel(_mediator.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void DropshipDashboardVM_CanBeInstantiated()
    {
        var vm = new DropshipDashboardAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void DropshipOrdersVM_CanBeInstantiated()
    {
        var vm = new DropshipOrdersAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void HealthVM_CanBeInstantiated()
    {
        var vm = new HealthAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void ReturnsVM_CanBeInstantiated()
    {
        var vm = new ReturnsAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void QuotationsVM_CanBeInstantiated()
    {
        var vm = new QuotationsAvaloniaViewModel(_mediator.Object, _currentUser.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void StockMovementsVM_CanBeInstantiated()
    {
        var vm = new StockMovementsAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void CommissionRatesVM_CanBeInstantiated()
    {
        var vm = new CommissionRatesViewModel(_mediator.Object, _currentUser.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void BankAccountsVM_CanBeInstantiated()
    {
        var vm = new BankAccountsAvaloniaViewModel(_mediator.Object, _currentUser.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }
}
