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
/// Success (green): Basarili, Tamamlandi, Kazaniyor, Aktif, Healthy, Connected, Gonderildi, Teslim Edildi, Onaylandi
/// Error (red): Hatali, Iptal, Kaybediyor, Error, Hata, Disconnected, Cancelled, Tukendi, Kritik, Failed
/// Warning (amber): Bekliyor, Hazirlaniyor, Warning, Uyari, Beklemede, Processing, Pending, Dusuk, Iade, Eksik
/// Info (blue): Yeni, New, Kargoda, Shipped, In_Transit, Bilgi, Info
/// Neutral (gray): Bilinmiyor, Pasif, Unknown, Inactive, Baglanti Yok, Taslak, Draft
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
            // Success (green)
            "basarili" or "tamamlandi" or "kazaniyor" or "aktif"
                or "healthy" or "connected" or "gonderildi"
                or "teslim edildi" or "onaylandi" or "success"
                => Token("MesConnectedGreen"),

            // Error (red)
            "hatali" or "iptal" or "kaybediyor" or "error"
                or "hata" or "disconnected" or "cancelled"
                or "tukendi" or "kritik" or "failed"
                => Token("MesDangerRed"),

            // Warning (amber)
            "bekliyor" or "hazirlaniyor" or "warning" or "uyari"
                or "beklemede" or "processing" or "pending"
                or "dusuk" or "iade" or "eksik"
                => Token("MesAmber"),

            // Info (blue)
            "yeni" or "new" or "kargoda" or "shipped"
                or "in_transit" or "bilgi" or "info"
                => Token("MesPrimaryBlue"),

            // Neutral (gray)
            "bilinmiyor" or "pasif" or "unknown" or "inactive"
                or "baglanti yok" or "taslak" or "draft"
                => Token("MesSlateGray"),

            _ => Token("MesSlateGray")
        };

        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
