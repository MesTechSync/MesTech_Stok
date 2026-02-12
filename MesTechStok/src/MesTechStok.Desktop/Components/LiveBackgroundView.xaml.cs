using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// Interaction logic for LiveBackgroundView.xaml
    /// </summary>
    public partial class LiveBackgroundView : UserControl
    {
        public LiveBackgroundView()
        {
            InitializeComponent();
            StartBackgroundAnimation();
        }

        private void StartBackgroundAnimation()
        {
            try
            {
                // Basit gradient animasyonu
                var gradientBrush = new LinearGradientBrush();
                gradientBrush.GradientStops.Add(new GradientStop(Colors.LightBlue, 0.0));
                gradientBrush.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));

                Background = gradientBrush;

                // Renk animasyonu
                var colorAnimation = new ColorAnimation
                {
                    From = Colors.LightBlue,
                    To = Colors.LightSteelBlue,
                    Duration = TimeSpan.FromSeconds(3),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };

                gradientBrush.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, colorAnimation);
            }
            catch (Exception ex)
            {
                // Animasyon başarısız olursa basit arka plan kullan
                Background = new SolidColorBrush(Colors.LightGray);
                System.Diagnostics.Debug.WriteLine($"Arka plan animasyonu başlatılamadı: {ex.Message}");
            }
        }
    }
}