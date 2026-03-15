using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Converters
{
    /// <summary>
    /// Converts an e-invoice status string to a background or foreground brush.
    /// Use BrushTarget="Background" or BrushTarget="Foreground".
    /// </summary>
    public class EInvoiceStatusToBrushConverter : IValueConverter
    {
        /// <summary>Target brush role: "Background" (default) or "Foreground".</summary>
        public string BrushTarget { get; set; } = "Background";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string ?? "";
            bool isFg = string.Equals(BrushTarget, "Foreground", StringComparison.OrdinalIgnoreCase);

            return status switch
            {
                "Draft"     => isFg ? Brush("#475569") : Brush("#f1f5f9"),
                "Sending"   => isFg ? Brush("#854d0e") : Brush("#fef9c3"),
                "Sent"      => isFg ? Brush("#1e40af") : Brush("#dbeafe"),
                "Accepted"  => isFg ? Brush("#166534") : Brush("#dcfce7"),
                "Rejected"  => isFg ? Brush("#991b1b") : Brush("#fee2e2"),
                "Cancelled" => isFg ? Brush("#6b7280") : Brush("#f1f5f9"),
                "Error"     => isFg ? Brush("#991b1b") : Brush("#fee2e2"),
                _           => isFg ? Brush("#374151") : Brush("#f3f4f6"),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        // ── helper ────────────────────────────────────────────────────────
        private static SolidColorBrush Brush(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = System.Convert.ToByte(hex[0..2], 16);
            byte g = System.Convert.ToByte(hex[2..4], 16);
            byte b = System.Convert.ToByte(hex[4..6], 16);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
