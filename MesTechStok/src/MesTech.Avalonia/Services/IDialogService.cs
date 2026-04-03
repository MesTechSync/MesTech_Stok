using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DialogHostAvalonia;

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
/// Avalonia dialog service using DialogHost.Avalonia for in-page overlay dialogs.
/// Replaces Window.ShowDialog with modern Material Design-style dialogs.
/// </summary>
public class AvaloniaDialogService : IDialogService
{
    private const string RootDialogIdentifier = "RootDialog";

    public async Task ShowInfoAsync(string message, string title)
    {
        try
        {
            var content = BuildInfoContent(title, message);
            await DialogHost.Show(content, RootDialogIdentifier);
        }
        catch (Exception ex)
        {
            // Fallback: headless/test ortamında DialogHost olmayabilir
            System.Diagnostics.Debug.WriteLine($"[DialogHost] {title}: {message} (fallback: {ex.Message})");
        }
    }

    public async Task<bool> ShowConfirmAsync(string message, string title)
    {
        try
        {
            var content = BuildConfirmContent(title, message);
            var result = await DialogHost.Show(content, RootDialogIdentifier);
            return result is true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DialogHost] {title}: {message} -> fallback false ({ex.Message})");
            return false;
        }
    }

    private static Border BuildInfoContent(string title, string message)
    {
        var closeButton = new Button
        {
            Content = "Tamam",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new global::Avalonia.Thickness(24, 8),
            Classes = { "accent" },
        };
        closeButton.Click += (_, _) => DialogHost.Close(RootDialogIdentifier);

        return new Border
        {
            Padding = new global::Avalonia.Thickness(32, 24),
            MinWidth = 380,
            MaxWidth = 520,
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 18,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#1A1A2E")),
                    },
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#6B7280")),
                        Margin = new global::Avalonia.Thickness(0, 0, 0, 16),
                    },
                    closeButton,
                }
            }
        };
    }

    private static Border BuildConfirmContent(string title, string message)
    {
        var yesBtn = new Button
        {
            Content = "Evet",
            Padding = new global::Avalonia.Thickness(24, 8),
            Classes = { "accent" },
        };
        var noBtn = new Button
        {
            Content = "Hayir",
            Padding = new global::Avalonia.Thickness(24, 8),
        };

        yesBtn.Click += (_, _) => DialogHost.Close(RootDialogIdentifier, true);
        noBtn.Click += (_, _) => DialogHost.Close(RootDialogIdentifier, false);

        return new Border
        {
            Padding = new global::Avalonia.Thickness(32, 24),
            MinWidth = 380,
            MaxWidth = 520,
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 18,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#1A1A2E")),
                    },
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#6B7280")),
                        Margin = new global::Avalonia.Thickness(0, 0, 0, 16),
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { noBtn, yesBtn }
                    }
                }
            }
        };
    }
}
