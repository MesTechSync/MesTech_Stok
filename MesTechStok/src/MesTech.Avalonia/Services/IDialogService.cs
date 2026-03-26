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
/// Avalonia-native dialog service using Window.ShowDialog.
/// Shows real confirmation dialogs to users — no auto-confirm.
/// </summary>
public class AvaloniaDialogService : IDialogService
{
    public async Task ShowInfoAsync(string message, string title)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            System.Diagnostics.Debug.WriteLine($"[{title}] {message}");
            return;
        }

        var dialog = new global::Avalonia.Controls.Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = global::Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new global::Avalonia.Controls.StackPanel
            {
                Margin = new global::Avalonia.Thickness(24),
                Spacing = 16,
                Children =
                {
                    new global::Avalonia.Controls.TextBlock
                    {
                        Text = message,
                        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                        FontSize = 14
                    },
                    new global::Avalonia.Controls.Button
                    {
                        Content = "Tamam",
                        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                        Padding = new global::Avalonia.Thickness(24, 8),
                    }
                }
            }
        };

        var okButton = ((global::Avalonia.Controls.StackPanel)dialog.Content).Children[1] as global::Avalonia.Controls.Button;
        okButton!.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(window);
    }

    public async Task<bool> ShowConfirmAsync(string message, string title)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            System.Diagnostics.Debug.WriteLine($"[{title}] {message} -> no window, defaulting false");
            return false;
        }

        var result = false;

        var dialog = new global::Avalonia.Controls.Window
        {
            Title = title,
            Width = 450,
            Height = 200,
            WindowStartupLocation = global::Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var yesBtn = new global::Avalonia.Controls.Button
        {
            Content = "Evet",
            Padding = new global::Avalonia.Thickness(24, 8),
        };
        var noBtn = new global::Avalonia.Controls.Button
        {
            Content = "Hayir",
            Padding = new global::Avalonia.Thickness(24, 8),
        };

        yesBtn.Click += (_, _) => { result = true; dialog.Close(); };
        noBtn.Click += (_, _) => { result = false; dialog.Close(); };

        dialog.Content = new global::Avalonia.Controls.StackPanel
        {
            Margin = new global::Avalonia.Thickness(24),
            Spacing = 16,
            Children =
            {
                new global::Avalonia.Controls.TextBlock
                {
                    Text = message,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14
                },
                new global::Avalonia.Controls.StackPanel
                {
                    Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { yesBtn, noBtn }
                }
            }
        };

        await dialog.ShowDialog(window);
        return result;
    }

    private static global::Avalonia.Controls.Window? GetMainWindow()
    {
        return global::Avalonia.Application.Current?.ApplicationLifetime is
            global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}
