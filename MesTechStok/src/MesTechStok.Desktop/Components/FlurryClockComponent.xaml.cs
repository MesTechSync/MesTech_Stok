using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MesTechStok.Desktop.Components
{
    public partial class FlurryClockComponent : UserControl
    {
        private DispatcherTimer _clockTimer = null!;
        private DispatcherTimer _weatherTimer = null!;
        private readonly CultureInfo _turkishCulture;
        private readonly Random _random;

        // Weather conditions with beautiful cloud icons
        private readonly (string icon, string description, int baseTemp)[] _weatherConditions =
        {
            ("☀️", "Güneşli", 25),
            ("⛅", "Az Bulutlu", 22),
            ("☁️", "Bulutlu", 18),
            ("🌤️", "Parçalı Bulutlu", 20),
            ("🌦️", "Hafif Yağmur", 15),
            ("🌧️", "Yağmurlu", 12),
            ("⛈️", "Fırtınalı", 10),
            ("🌨️", "Karlı", 2),
            ("🌫️", "Sisli", 8),
            ("❄️", "Soğuk", 0)
        };

        public FlurryClockComponent()
        {
            InitializeComponent();
            _turkishCulture = new CultureInfo("tr-TR");
            _random = new Random();
            InitializeTimers();
            UpdateDisplay();
            StartAnimations();
        }

        private void InitializeTimers()
        {
            // Clock timer - updates every second
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Weather timer - updates every 3 minutes for demo
            _weatherTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(3)
            };
            _weatherTimer.Tick += WeatherTimer_Tick;
            _weatherTimer.Start();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimeDisplay();
        }

        private void WeatherTimer_Tick(object? sender, EventArgs e)
        {
            UpdateWeatherDisplay();
        }

        private void UpdateDisplay()
        {
            UpdateTimeDisplay();
            UpdateWeatherDisplay();
        }

        private void UpdateTimeDisplay()
        {
            try
            {
                var currentTime = DateTime.Now;

                // Modern yapıdaki ayrı TextBlock'ları güncelle
                if (FindName("HourText") is TextBlock hourBlock)
                {
                    hourBlock.Text = currentTime.ToString("HH");
                }

                if (FindName("MinuteText") is TextBlock minuteBlock)
                {
                    minuteBlock.Text = currentTime.ToString("mm");
                }

                if (FindName("DateText") is TextBlock dateBlock)
                {
                    dateBlock.Text = currentTime.ToString("dd MMMM yyyy, dddd", _turkishCulture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Time update error: {ex.Message}");
            }
        }

        private void UpdateWeatherDisplay()
        {
            try
            {
                // Select random weather condition for demo
                var selectedWeather = _weatherConditions[_random.Next(_weatherConditions.Length)];

                // Add some randomness to temperature
                var temperature = selectedWeather.baseTemp + _random.Next(-5, 6);

                // Update weather display
                WeatherIcon.Text = selectedWeather.icon;
                TemperatureText.Text = $"{temperature}°";
                WeatherDescriptionText.Text = selectedWeather.description;
            }
            catch (Exception ex)
            {
                // Fallback in case of error
                WeatherIcon.Text = "❓";
                TemperatureText.Text = "--°";
                WeatherDescriptionText.Text = "Veri Yok";

                System.Diagnostics.Debug.WriteLine($"Weather update error: {ex.Message}");
            }
        }

        private void StartAnimations()
        {
            try
            {
                // Start glassmorphism animation
                var glassStoryboard = (Storyboard)Resources["GlassUpdateAnimation"];
                glassStoryboard?.Begin();

                // Start weather icon pulse animation
                var weatherStoryboard = (Storyboard)Resources["WeatherPulse"];
                weatherStoryboard?.Begin();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        // Public methods to update from external sources
        public void UpdateSystemStatus(string status)
        {
            SystemStatusText.Text = status;
        }

        public void SetOnlineStatus(bool isOnline)
        {
            // Update the online indicator color
            var indicator = (Ellipse)((StackPanel)SystemStatusText.Parent).Children[0];
            indicator.Fill = new SolidColorBrush(isOnline ? Colors.LimeGreen : Colors.Red);
        }

        public void UpdateWeatherData(string icon, int temperature, string description)
        {
            WeatherIcon.Text = icon;
            TemperatureText.Text = $"{temperature}°";
            WeatherDescriptionText.Text = description;
        }

        // Cleanup timer when control is unloaded
        private void FlurryClockComponent_Unloaded(object sender, RoutedEventArgs e)
        {
            _clockTimer?.Stop();
            _weatherTimer?.Stop();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Subscribe to unload event
            Unloaded += FlurryClockComponent_Unloaded;
        }
    }
}