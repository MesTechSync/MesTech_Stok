using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Typed factory for resolving ViewModels — replaces raw IServiceProvider
/// injection (ServiceLocator anti-pattern) in MainWindowViewModel.
/// Each ViewModel is resolved through the IHost DI container.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>
    /// Resolve a ViewModel by its navigation key (e.g. "Dashboard", "Products").
    /// Returns null if the key is not mapped to a registered ViewModel.
    /// </summary>
    ObservableObject? Create(string viewName);
}
