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
/// G050: Kritik MediatR-bağımlı ViewModel'lerin batch instantiation testi.
/// Her VM mock dependency'lerle oluşturulabilmeli.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
[Trait("Group", "AvaloniaVMBatch")]
public class AvaloniaViewModelBatchTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public void CargoVM_CanBeInstantiated()
    {
        var vm = new CargoAvaloniaViewModel(_dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void CrmDashboardVM_CanBeInstantiated()
    {
        var vm = new CrmDashboardAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void AccountingDashboardVM_CanBeInstantiated()
    {
        var vm = new AccountingDashboardAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void KanbanVM_CanBeInstantiated()
    {
        var vm = new KanbanAvaloniaViewModel(
            _mediator.Object, _currentUser.Object, _tenantProvider.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void AboutVM_CanBeInstantiated()
    {
        var vm = new AboutAvaloniaViewModel();
        vm.Should().NotBeNull();
    }

    [Fact]
    public void CalendarVM_CanBeInstantiated()
    {
        var vm = new CalendarAvaloniaViewModel(_mediator.Object, _currentUser.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void AuditLogVM_CanBeInstantiated()
    {
        var vm = new AuditLogAvaloniaViewModel(_mediator.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public void BackupVM_CanBeInstantiated()
    {
        var vm = new BackupAvaloniaViewModel();
        vm.Should().NotBeNull();
    }

    [Fact]
    public void BarcodeVM_CanBeInstantiated()
    {
        var vm = new BarcodeAvaloniaViewModel();
        vm.Should().NotBeNull();
    }

    [Fact]
    public void BulkProductVM_CanBeInstantiated()
    {
        var vm = new BulkProductAvaloniaViewModel(_mediator.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }
}
