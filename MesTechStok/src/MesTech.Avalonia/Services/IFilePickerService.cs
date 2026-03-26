using Avalonia.Platform.Storage;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Cross-platform file picker abstraction for Avalonia ViewModels.
/// Wraps Avalonia 11 IStorageProvider so ViewModels remain testable without Window references.
/// </summary>
public interface IFilePickerService
{
    /// <summary>Shows an OpenFilePicker dialog and returns the selected file path, or null if cancelled.</summary>
    Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType> fileTypes);
}

/// <summary>
/// Avalonia-native file picker using TopLevel.StorageProvider.
/// Requires the MainWindow to be set in the application lifetime.
/// </summary>
public class AvaloniaFilePickerService : IFilePickerService
{
    public async Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType> fileTypes)
    {
        var topLevel = GetTopLevel();
        if (topLevel == null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    private static global::Avalonia.Controls.TopLevel? GetTopLevel()
    {
        return global::Avalonia.Application.Current?.ApplicationLifetime is
            global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? global::Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)
            : null;
    }
}
