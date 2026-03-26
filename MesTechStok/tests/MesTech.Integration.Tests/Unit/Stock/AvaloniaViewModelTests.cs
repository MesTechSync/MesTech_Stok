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
/// G050: Avalonia ViewModel unit testleri — kritik 5 VM.
/// ViewModel constructor'ları çağrılabilir mi, LoadAsync exception fırlatmaz mı?
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
[Trait("Group", "AvaloniaVM")]
public class AvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IDialogService> _dialog = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    // ═══ SettingsAvaloniaViewModel ═══

    [Fact]
    public void SettingsVM_CanBeInstantiated()
    {
        var vm = new SettingsAvaloniaViewModel(_mediator.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }

    [Fact]
    public async Task SettingsVM_LoadAsync_DoesNotThrow()
    {
        var vm = new SettingsAvaloniaViewModel(_mediator.Object, _dialog.Object);
        var act = () => vm.LoadAsync();
        await act.Should().NotThrowAsync();
    }

    // ═══ StockAvaloniaViewModel ═══

    [Fact]
    public void StockVM_CanBeInstantiated()
    {
        var vm = new StockAvaloniaViewModel();
        vm.Should().NotBeNull();
        vm.Summary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StockVM_LoadAsync_DoesNotThrow()
    {
        var vm = new StockAvaloniaViewModel();
        var act = () => vm.LoadAsync();
        await act.Should().NotThrowAsync();
    }

    // ═══ DashboardAvaloniaViewModel ═══

    [Fact]
    public void DashboardVM_CanBeInstantiated()
    {
        var vm = new DashboardAvaloniaViewModel(_mediator.Object, _tenantProvider.Object);
        vm.Should().NotBeNull();
    }

    // ═══ OnboardingWizardAvaloniaViewModel ═══

    [Fact]
    public void OnboardingWizardVM_CanBeInstantiated()
    {
        var vm = new OnboardingWizardAvaloniaViewModel(_mediator.Object);
        vm.Should().NotBeNull();
    }

    // ═══ LeadsAvaloniaViewModel ═══

    [Fact]
    public void LeadsVM_CanBeInstantiated()
    {
        var vm = new LeadsAvaloniaViewModel(_mediator.Object, _currentUser.Object, _dialog.Object);
        vm.Should().NotBeNull();
    }
}
