using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Components
{
    /// <summary>
    /// Interaction logic for WeatherWidgetView.xaml
    /// </summary>
    public partial class WeatherWidgetView : UserControl
    {
        public WeatherWidgetView()
        {
            InitializeComponent();
            LoadWeatherData();
        }

        private async void LoadWeatherData()
        {
            try
            {
                // Mock hava durumu verisi
                await Task.Delay(500); // API çağrısını simüle et

                if (FindName("TemperatureTextBlock") is TextBlock tempBlock)
                {
                    tempBlock.Text = "22°C";
                }

                if (FindName("ConditionTextBlock") is TextBlock conditionBlock)
                {
                    conditionBlock.Text = "Güneşli";
                }

                if (FindName("LocationTextBlock") is TextBlock locationBlock)
                {
                    locationBlock.Text = "İstanbul";
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda default değerler
                if (FindName("TemperatureTextBlock") is TextBlock tempBlock)
                {
                    tempBlock.Text = "--°C";
                }

                if (FindName("ConditionTextBlock") is TextBlock conditionBlock)
                {
                    conditionBlock.Text = "Veri yok";
                }

                System.Diagnostics.Debug.WriteLine($"Hava durumu yüklenirken hata: {ex.Message}");
            }
        }

        private void InitializeWeatherData()
        {
            // Simple mock weather data for demonstration
            // In real implementation, this would fetch from a weather API
            LoadMockWeatherData();
        }

        private void LoadMockWeatherData()
        {
            try
            {
                // Mock weather data for Turkish cities
                var random = new Random();
                var temperature = random.Next(5, 35);
                var conditions = new[]
                {
                    ("☀", "Güneşli", "#FFD700"),
                    ("⛅", "Parçalı Bulutlu", "#87CEEB"),
                    ("☁", "Bulutlu", "#778899"),
                    ("🌧", "Yağmurlu", "#4682B4"),
                    ("❄", "Karlı", "#E0E0E0"),
                    ("🌩", "Fırtınalı", "#2F4F4F")
                };

                var cities = new[] { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya" };

                var selectedCondition = conditions[random.Next(conditions.Length)];
                var selectedCity = cities[random.Next(cities.Length)];

                // Update UI elements
                CityTextBlock.Text = selectedCity;
                TemperatureTextBlock.Text = $"{temperature}°";
                WeatherIcon.Text = selectedCondition.Item1;
                DescriptionTextBlock.Text = selectedCondition.Item2;

                // Update last updated time
                LastUpdatedTextBlock.Text = $"Son güncelleme: {DateTime.Now:HH:mm}";

                // Change background color based on weather
                if (selectedCondition.Item3 != null)
                {
                    // This would change the gradient colors based on weather condition
                    // For now, keeping the default blue gradient
                }
            }
            catch (Exception ex)
            {
                // Handle error gracefully
                ShowErrorState($"Hava durumu yüklenemiyor: {ex.Message}");
            }
        }

        private void ShowErrorState(string errorMessage)
        {
            CityTextBlock.Text = "Hata";
            TemperatureTextBlock.Text = "--°";
            WeatherIcon.Text = "⚠";
            DescriptionTextBlock.Text = "Veri Yok";
            LastUpdatedTextBlock.Text = errorMessage;
        }

        public void RefreshWeather()
        {
            ShowLoadingState();

            // Simulate loading delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                HideLoadingState();
                LoadMockWeatherData();
            };
            timer.Start();
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            var storyboard = (System.Windows.Media.Animation.Storyboard)Resources["LoadingAnimation"];
            storyboard?.Begin();
        }

        private void HideLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            var storyboard = (System.Windows.Media.Animation.Storyboard)Resources["LoadingAnimation"];
            storyboard?.Stop();
        }

        // Public method to update city
        public void SetCity(string cityName)
        {
            if (!string.IsNullOrEmpty(cityName))
            {
                CityTextBlock.Text = cityName;
                RefreshWeather();
            }
        }

        // Public method to update weather data (for future API integration)
        public void UpdateWeatherData(string city, int temperature, string condition, string icon)
        {
            CityTextBlock.Text = city;
            TemperatureTextBlock.Text = $"{temperature}°";
            DescriptionTextBlock.Text = condition;
            WeatherIcon.Text = icon;
            LastUpdatedTextBlock.Text = $"Son güncelleme: {DateTime.Now:HH:mm}";
        }
    }
}