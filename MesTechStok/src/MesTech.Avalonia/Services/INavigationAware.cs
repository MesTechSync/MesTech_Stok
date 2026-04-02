namespace MesTech.Avalonia.Services;

/// <summary>
/// Implement on ViewModels that accept navigation parameters.
/// Called by INavigationService after view switch completes.
/// </summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync(IDictionary<string, object?> parameters);
}
