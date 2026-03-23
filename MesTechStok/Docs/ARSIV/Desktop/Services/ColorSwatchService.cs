using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesTechStok.Desktop.Services
{
    public class ColorSwatchService
    {
        public async Task<string> GenerateAsync(string colorHex, int size = 256, string? label = null)
        {
            return await Task.Run(() =>
            {
                var color = (Color)ColorConverter.ConvertFromString(Normalize(colorHex));
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(brush, null, new Rect(0, 0, size, size));
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        var ft = new FormattedText(
                            label,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            18,
                            GetContrastBrush(color),
                            96);
                        dc.DrawText(ft, new Point(8, size - 30));
                    }
                }

                var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                rtb.Freeze();

                var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MesTechStok", "Temp");
                Directory.CreateDirectory(tempDir);
                var file = Path.Combine(tempDir, $"swatch_{Guid.NewGuid():N}.jpg");
                using (var fs = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var enc = new JpegBitmapEncoder { QualityLevel = 92 };
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    enc.Save(fs);
                }
                return file;
            });
        }

        private static string Normalize(string hex)
        {
            hex = (hex ?? "").Trim();
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (hex.Length == 4)
            {
                // #RGB -> #RRGGBB
                hex = "#" + new string(new[] { hex[1], hex[1], hex[2], hex[2], hex[3], hex[3] });
            }
            return hex;
        }

        private static Brush GetContrastBrush(Color c)
        {
            // YIQ contrast formula
            double yiq = ((c.R * 299) + (c.G * 587) + (c.B * 114)) / 1000.0;
            return yiq >= 128 ? Brushes.Black : Brushes.White;
        }
    }
}


