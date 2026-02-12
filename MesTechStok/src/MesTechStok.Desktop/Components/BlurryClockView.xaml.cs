using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// Interaction logic for BlurryClockView.xaml
    /// </summary>
    public partial class BlurryClockView : UserControl
    {
        private readonly DispatcherTimer _timer;

        public BlurryClockView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateTime();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            var now = DateTime.Now;

            // Modern yapıdaki ayrı TextBlock'ları güncelle
            if (FindName("HourText") is TextBlock hourBlock)
            {
                hourBlock.Text = now.ToString("HH");
            }

            if (FindName("MinuteText") is TextBlock minuteBlock)
            {
                minuteBlock.Text = now.ToString("mm");
            }

            if (FindName("SecondText") is TextBlock secondBlock)
            {
                secondBlock.Text = now.ToString("ss");
            }

            if (FindName("DateTextBlock") is TextBlock dateBlock)
            {
                dateBlock.Text = now.ToString("dd MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR"));
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
        }
    }
}