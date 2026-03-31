namespace MesTech.Avalonia.Services;

/// <summary>
/// Navigation service for Avalonia views — KÇ-14 compliant.
/// Allows ViewModels to trigger page navigation without direct MainWindow reference.
/// </summary>
public interface INavigationService
{
    Task NavigateToAsync(string viewName);
}
