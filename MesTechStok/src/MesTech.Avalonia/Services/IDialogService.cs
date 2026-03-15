namespace MesTech.Avalonia.Services;

/// <summary>
/// Cross-platform dialog abstraction replacing System.Windows.MessageBox.
/// WPF ViewModels that call MessageBox.Show() should migrate to this interface
/// to enable full compile-link reuse across WPF, Avalonia, and MAUI hosts.
/// </summary>
public interface IDialogService
{
    Task ShowInfoAsync(string message, string title);
    Task<bool> ShowConfirmAsync(string message, string title);
}

/// <summary>
/// Simple console-based dialog for PoC — replace with Avalonia dialog in production.
/// </summary>
public class ConsoleDialogService : IDialogService
{
    public Task ShowInfoAsync(string message, string title)
    {
        System.Diagnostics.Debug.WriteLine($"[{title}] {message}");
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmAsync(string message, string title)
    {
        System.Diagnostics.Debug.WriteLine($"[{title}] {message} -> auto-confirmed");
        return Task.FromResult(true);
    }
}
