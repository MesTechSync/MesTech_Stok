using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MesTech.Avalonia.Converters;

/// <summary>
/// Converts a status string to a theme-aware SolidColorBrush.
/// Replaces 100+ hardcoded hex colors in ViewModels with dark/light mode support.
/// Usage in AXAML: {Binding Status, Converter={StaticResource StatusToColorConverter}}
///
/// Recognized status strings (case-insensitive):
/// Success: "Basarili", "Tamamlandi", "Kazaniyor", "Aktif", "Healthy", "Connected"
/// Error: "Hatali", "Iptal", "Kaybediyor", "Error", "Hata"
/// Warning: "Bekliyor", "Hazirlaniyor", "Warning", "Uyari", "Beklemede"
/// Info: "Bilinmiyor", "Pasif", default
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    private static Color Token(string key) =>
        global::Avalonia.Application.Current?.Resources.TryGetResource(key, null, out var val) == true && val is Color c ? c : Colors.Gray;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = (value?.ToString() ?? string.Empty).Trim().ToLowerInvariant();

        // Direct hex pass-through (for backward compat during migration)
        if (status.StartsWith('#') && status.Length == 7)
            return new SolidColorBrush(Color.Parse(status));

        var color = status switch
        {
            "basarili" or "tamamlandi" or "kazaniyor" or "aktif"
                or "healthy" or "connected" or "gonderildi"
                => Token("MesConnectedGreen"),

            "hatali" or "iptal" or "kaybediyor" or "error"
                or "hata" or "disconnected" or "cancelled"
                => Token("MesDangerRed"),

            "bekliyor" or "hazirlaniyor" or "warning" or "uyari"
                or "beklemede" or "processing" or "pending"
                => Token("MesAmber"),

            "bilinmiyor" or "pasif" or "unknown" or "inactive"
                => Token("MesSlateGray"),

            _ => Token("MesSlateGray")
        };

        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
