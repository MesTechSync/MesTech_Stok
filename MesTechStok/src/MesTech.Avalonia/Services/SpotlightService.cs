using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Spotlight image discovery and rotation service.
/// Scans Resources/Spotlights/ and %LOCALAPPDATA%/MesTech/Spotlights/ for background images.
///
/// KORUMALI: Klasör tarama mantığı ve thread-safety değiştirilemez.
/// İZİN: Yeni format (.avif, .svg), metadata zenginleştirme, cache stratejisi.
/// Thread-safe index tracking for crossfade rotation.
/// </summary>
public sealed class SpotlightService
{
    private readonly List<SpotlightImageInfo> _images = new();
    private int _currentIndex;
    private readonly object _lock = new();

    private static readonly string[] SupportedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

    public SpotlightService()
    {
        ScanFolders();
    }

    /// <summary>Total discovered images.</summary>
    public int Count => _images.Count;

    /// <summary>True if at least one image was found.</summary>
    public bool HasImages => _images.Count > 0;

    /// <summary>Current rotation index.</summary>
    public int CurrentIndex
    {
        get { lock (_lock) return _currentIndex; }
    }

    /// <summary>Get all discovered images (for thumbnail bar).</summary>
    public IReadOnlyList<SpotlightImageInfo> GetAll() => _images.AsReadOnly();

    /// <summary>Get current image info, or null if no images.</summary>
    public SpotlightImageInfo? GetCurrent()
    {
        lock (_lock)
        {
            if (_images.Count == 0) return null;
            return _images[_currentIndex % _images.Count];
        }
    }

    /// <summary>Advance to next image and return it. Wraps around.</summary>
    public SpotlightImageInfo? GetNext()
    {
        lock (_lock)
        {
            if (_images.Count == 0) return null;
            _currentIndex = (_currentIndex + 1) % _images.Count;
            return _images[_currentIndex];
        }
    }

    /// <summary>Go to previous image and return it. Wraps around.</summary>
    public SpotlightImageInfo? GetPrevious()
    {
        lock (_lock)
        {
            if (_images.Count == 0) return null;
            _currentIndex = (_currentIndex - 1 + _images.Count) % _images.Count;
            return _images[_currentIndex];
        }
    }

    /// <summary>Jump to a specific index.</summary>
    public SpotlightImageInfo? GoTo(int index)
    {
        lock (_lock)
        {
            if (_images.Count == 0) return null;
            _currentIndex = Math.Clamp(index, 0, _images.Count - 1);
            return _images[_currentIndex];
        }
    }

    /// <summary>Rescan folders for new images.</summary>
    public void Refresh()
    {
        lock (_lock)
        {
            _images.Clear();
            _currentIndex = 0;
        }
        ScanFolders();
    }

    private void ScanFolders()
    {
        var folders = new List<string>();

        // Primary: app directory
        var appDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Spotlights");
        if (Directory.Exists(appDir))
            folders.Add(appDir);

        // Secondary: user local app data
        var localDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MesTech", "Spotlights");
        if (Directory.Exists(localDir))
            folders.Add(localDir);

        var index = 0;
        foreach (var folder in folders)
        {
            try
            {
                var files = Directory.GetFiles(folder)
                    .Where(f => SupportedExtensions.Contains(
                        Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f);

                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file)
                        .Replace('_', ' ')
                        .Replace('-', ' ');

                    // Capitalize first letter of each word
                    name = string.Join(' ', name.Split(' ')
                        .Select(w => w.Length > 0
                            ? char.ToUpper(w[0]) + w[1..]
                            : w));

                    lock (_lock)
                    {
                        _images.Add(new SpotlightImageInfo(file, name, index++));
                    }
                }
            }
            catch
            {
                // Folder scan failure is non-critical — skip silently
            }
        }
    }
}

/// <summary>Metadata for a single spotlight background image.</summary>
public sealed record SpotlightImageInfo(string FilePath, string DisplayName, int Index);
