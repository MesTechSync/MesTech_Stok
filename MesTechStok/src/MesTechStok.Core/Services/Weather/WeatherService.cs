using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTechStok.Core.Services.Weather
{
    /// <summary>
    /// Hava durumu bilgisi modeli
    /// </summary>
    public class WeatherInfo
    {
        public string Location { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public double Pressure { get; set; }
        public double Visibility { get; set; }
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
        public string Provider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hava durumu tahmin modeli
    /// </summary>
    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double ChanceOfRain { get; set; }
    }

    /// <summary>
    /// Hava durumu servis ayarları
    /// </summary>
    public class WeatherApiSettings
    {
        public string OpenWeatherMapApiKey { get; set; } = string.Empty;
        public string DefaultCity { get; set; } = "Istanbul";
        public string DefaultCountryCode { get; set; } = "TR";
        public string Language { get; set; } = "tr";
        public string Units { get; set; } = "metric"; // metric, imperial, kelvin
        public int CacheExpirationMinutes { get; set; } = 30;
        public bool EnableForecast { get; set; } = true;
    }

    /// <summary>
    /// Hava durumu servis interface
    /// </summary>
    public interface IWeatherService
    {
        Task<WeatherInfo?> GetCurrentWeatherAsync(string? city = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<WeatherForecast>> GetForecastAsync(string? city = null, int days = 5, CancellationToken cancellationToken = default);
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
        Task<WeatherInfo?> GetWeatherByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// OpenWeatherMap API entegrasyonu
    /// Gerçek hava durumu verisi için
    /// </summary>
    public class OpenWeatherMapService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenWeatherMapService> _logger;
        private readonly WeatherApiSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/";
        private static readonly Dictionary<string, WeatherInfo> _cache = new();
        private static readonly Dictionary<string, DateTime> _cacheExpiry = new();

        public OpenWeatherMapService(
            HttpClient httpClient,
            ILogger<OpenWeatherMapService> logger,
            IOptions<WeatherApiSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Güncel hava durumunu getirir
        /// </summary>
        public async Task<WeatherInfo?> GetCurrentWeatherAsync(string? city = null, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            var location = city ?? _settings.DefaultCity;
            var cacheKey = $"current_{location}";

            try
            {
                // Cache kontrolü
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogDebug("[WeatherAPI] Returning cached weather data for {Location}", location);
                    return _cache[cacheKey];
                }

                var requestUrl = $"weather?q={Uri.EscapeDataString(location)}&appid={_settings.OpenWeatherMapApiKey}&units={_settings.Units}&lang={_settings.Language}";

                _logger.LogDebug("[WeatherAPI] Fetching current weather for {Location}, CorrelationId={CorrelationId}",
                    location, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[WeatherAPI] API request failed with status {StatusCode} for {Location}",
                        response.StatusCode, location);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherCurrentResponse>(jsonContent, _jsonOptions);

                if (weatherData == null)
                {
                    _logger.LogWarning("[WeatherAPI] Failed to parse weather data for {Location}", location);
                    return null;
                }

                var weatherInfo = new WeatherInfo
                {
                    Location = weatherData.Name ?? location,
                    Temperature = weatherData.Main?.Temp ?? 0,
                    FeelsLike = weatherData.Main?.FeelsLike ?? 0,
                    Humidity = weatherData.Main?.Humidity ?? 0,
                    Description = weatherData.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                    Icon = weatherData.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                    WindSpeed = weatherData.Wind?.Speed ?? 0,
                    WindDirection = weatherData.Wind?.Deg ?? 0,
                    Pressure = weatherData.Main?.Pressure ?? 0,
                    Visibility = (weatherData.Visibility ?? 0) / 1000.0, // Convert to km
                    UpdateTime = DateTime.UtcNow,
                    Provider = "OpenWeatherMap"
                };

                // Cache'e kaydet
                _cache[cacheKey] = weatherInfo;
                _cacheExpiry[cacheKey] = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes);

                _logger.LogInformation("[WeatherAPI] Successfully fetched weather for {Location}: {Temperature}°C, {Description}",
                    location, weatherInfo.Temperature, weatherInfo.Description);

                return weatherInfo;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Network error while fetching weather for {Location}", location);
                return GetFallbackWeatherData(location);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Request timeout while fetching weather for {Location}", location);
                return GetFallbackWeatherData(location);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[WeatherAPI] JSON parsing error while fetching weather for {Location}", location);
                return GetFallbackWeatherData(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Unexpected error while fetching weather for {Location}", location);
                return GetFallbackWeatherData(location);
            }
        }

        /// <summary>
        /// Koordinatlara göre hava durumu getirir
        /// </summary>
        public async Task<WeatherInfo?> GetWeatherByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            var cacheKey = $"coords_{latitude:F2}_{longitude:F2}";

            try
            {
                // Cache kontrolü
                if (IsCacheValid(cacheKey))
                {
                    _logger.LogDebug("[WeatherAPI] Returning cached weather data for coordinates {Lat},{Lon}", latitude, longitude);
                    return _cache[cacheKey];
                }

                var requestUrl = $"weather?lat={latitude}&lon={longitude}&appid={_settings.OpenWeatherMapApiKey}&units={_settings.Units}&lang={_settings.Language}";

                _logger.LogDebug("[WeatherAPI] Fetching weather for coordinates {Lat},{Lon}, CorrelationId={CorrelationId}",
                    latitude, longitude, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var weatherData = JsonSerializer.Deserialize<OpenWeatherCurrentResponse>(jsonContent, _jsonOptions);

                if (weatherData == null) return null;

                var weatherInfo = new WeatherInfo
                {
                    Location = weatherData.Name ?? $"{latitude:F2},{longitude:F2}",
                    Temperature = weatherData.Main?.Temp ?? 0,
                    FeelsLike = weatherData.Main?.FeelsLike ?? 0,
                    Humidity = weatherData.Main?.Humidity ?? 0,
                    Description = weatherData.Weather?.FirstOrDefault()?.Description ?? string.Empty,
                    Icon = weatherData.Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                    WindSpeed = weatherData.Wind?.Speed ?? 0,
                    WindDirection = weatherData.Wind?.Deg ?? 0,
                    Pressure = weatherData.Main?.Pressure ?? 0,
                    Visibility = (weatherData.Visibility ?? 0) / 1000.0,
                    UpdateTime = DateTime.UtcNow,
                    Provider = "OpenWeatherMap"
                };

                // Cache'e kaydet
                _cache[cacheKey] = weatherInfo;
                _cacheExpiry[cacheKey] = DateTime.UtcNow.AddMinutes(_settings.CacheExpirationMinutes);

                return weatherInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Error fetching weather for coordinates {Lat},{Lon}", latitude, longitude);
                return null;
            }
        }

        /// <summary>
        /// Hava durumu tahminlerini getirir
        /// </summary>
        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(string? city = null, int days = 5, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            if (!_settings.EnableForecast)
            {
                _logger.LogDebug("[WeatherAPI] Forecast is disabled in settings");
                return new List<WeatherForecast>();
            }

            var location = city ?? _settings.DefaultCity;

            try
            {
                var requestUrl = $"forecast?q={Uri.EscapeDataString(location)}&appid={_settings.OpenWeatherMapApiKey}&units={_settings.Units}&lang={_settings.Language}&cnt={days * 8}"; // 8 forecasts per day (3-hour intervals)

                _logger.LogDebug("[WeatherAPI] Fetching {Days}-day forecast for {Location}, CorrelationId={CorrelationId}",
                    days, location, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var forecastData = JsonSerializer.Deserialize<OpenWeatherForecastResponse>(jsonContent, _jsonOptions);

                if (forecastData?.List == null)
                {
                    _logger.LogWarning("[WeatherAPI] Failed to parse forecast data for {Location}", location);
                    return new List<WeatherForecast>();
                }

                // Group by date and get daily summaries
                var dailyForecasts = forecastData.List
                    .GroupBy(f => DateTimeExtensions.UnixTimeStamp(f.Dt).Date)
                    .Take(days)
                    .Select(g => new WeatherForecast
                    {
                        Date = g.Key,
                        MinTemperature = g.Min(f => f.Main?.TempMin ?? 0),
                        MaxTemperature = g.Max(f => f.Main?.TempMax ?? 0),
                        Description = g.First().Weather?.FirstOrDefault()?.Description ?? string.Empty,
                        Icon = g.First().Weather?.FirstOrDefault()?.Icon ?? string.Empty,
                        Humidity = (int)g.Average(f => f.Main?.Humidity ?? 0),
                        WindSpeed = g.Average(f => f.Wind?.Speed ?? 0),
                        ChanceOfRain = g.Average(f => f.Pop ?? 0) * 100
                    })
                    .ToList();

                _logger.LogInformation("[WeatherAPI] Successfully fetched {Count}-day forecast for {Location}",
                    dailyForecasts.Count, location);

                return dailyForecasts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Error fetching forecast for {Location}", location);
                return new List<WeatherForecast>();
            }
        }

        /// <summary>
        /// API bağlantı testi
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var weather = await GetCurrentWeatherAsync(_settings.DefaultCity, cancellationToken);
                return weather != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WeatherAPI] Connection test failed");
                return false;
            }
        }

        #region Private Methods

        private bool IsCacheValid(string cacheKey)
        {
            return _cache.ContainsKey(cacheKey) &&
                   _cacheExpiry.ContainsKey(cacheKey) &&
                   _cacheExpiry[cacheKey] > DateTime.UtcNow;
        }

        private WeatherInfo GetFallbackWeatherData(string location)
        {
            // Cache'den eskiyi döndür veya varsayılan veri
            if (_cache.ContainsKey($"current_{location}"))
            {
                _logger.LogInformation("[WeatherAPI] Returning cached fallback weather data for {Location}", location);
                return _cache[$"current_{location}"];
            }

            _logger.LogInformation("[WeatherAPI] Returning default fallback weather data for {Location}", location);

            return new WeatherInfo
            {
                Location = location,
                Temperature = 20,
                FeelsLike = 20,
                Humidity = 50,
                Description = "Veri alınamadı",
                Icon = "01d",
                WindSpeed = 5,
                WindDirection = 180,
                Pressure = 1013,
                Visibility = 10,
                UpdateTime = DateTime.UtcNow,
                Provider = "Fallback"
            };
        }

        #endregion
    }

    #region OpenWeatherMap API Models

    public class OpenWeatherCurrentResponse
    {
        public string? Name { get; set; }
        public WeatherMain? Main { get; set; }
        public List<WeatherCondition>? Weather { get; set; }
        public WeatherWind? Wind { get; set; }
        public long? Visibility { get; set; }
    }

    public class OpenWeatherForecastResponse
    {
        public List<OpenWeatherForecastItem>? List { get; set; }
    }

    public class OpenWeatherForecastItem
    {
        public long Dt { get; set; }
        public WeatherMain? Main { get; set; }
        public List<WeatherCondition>? Weather { get; set; }
        public WeatherWind? Wind { get; set; }
        public double? Pop { get; set; } // Probability of precipitation
    }

    public class WeatherMain
    {
        public double Temp { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Humidity { get; set; }
        public double Pressure { get; set; }
    }

    public class WeatherCondition
    {
        public string? Main { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
    }

    public class WeatherWind
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
    }

    public static class DateTimeExtensions
    {
        public static DateTime UnixTimeStamp(long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).DateTime;
        }
    }

    #endregion

    /// <summary>
    /// Mock hava durumu servisi (geliştirme için)
    /// </summary>
    public class MockWeatherService : IWeatherService
    {
        private readonly ILogger<MockWeatherService> _logger;
        private readonly Random _random = new();

        public MockWeatherService(ILogger<MockWeatherService> logger)
        {
            _logger = logger;
        }

        public async Task<WeatherInfo?> GetCurrentWeatherAsync(string? city = null, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate API delay

            var location = city ?? "İstanbul";
            var temperature = 15 + _random.NextDouble() * 20; // 15-35°C

            var descriptions = new[] { "Açık", "Parçalı bulutlu", "Bulutlu", "Yağmurlu", "Sisli" };
            var icons = new[] { "01d", "02d", "03d", "09d", "50d" };

            var desc = descriptions[_random.Next(descriptions.Length)];
            var icon = icons[_random.Next(icons.Length)];

            _logger.LogDebug("[WeatherAPI] Generated mock weather for {Location}: {Temperature}°C", location, temperature);

            return new WeatherInfo
            {
                Location = location,
                Temperature = Math.Round(temperature, 1),
                FeelsLike = Math.Round(temperature + _random.NextDouble() * 4 - 2, 1),
                Humidity = 40 + _random.Next(40),
                Description = desc,
                Icon = icon,
                WindSpeed = _random.NextDouble() * 20,
                WindDirection = _random.Next(360),
                Pressure = 1000 + _random.NextDouble() * 50,
                Visibility = 5 + _random.NextDouble() * 10,
                UpdateTime = DateTime.UtcNow,
                Provider = "Mock"
            };
        }

        public async Task<WeatherInfo?> GetWeatherByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await GetCurrentWeatherAsync($"{latitude:F2},{longitude:F2}", cancellationToken);
        }

        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(string? city = null, int days = 5, CancellationToken cancellationToken = default)
        {
            await Task.Delay(200, cancellationToken);

            var forecasts = new List<WeatherForecast>();

            for (int i = 0; i < days; i++)
            {
                var baseTemp = 20 + _random.NextDouble() * 15;
                forecasts.Add(new WeatherForecast
                {
                    Date = DateTime.Today.AddDays(i),
                    MinTemperature = Math.Round(baseTemp - 5, 1),
                    MaxTemperature = Math.Round(baseTemp + 5, 1),
                    Description = "Mock forecast",
                    Icon = "01d",
                    Humidity = 50 + _random.Next(30),
                    WindSpeed = _random.NextDouble() * 15,
                    ChanceOfRain = _random.NextDouble() * 100
                });
            }

            return forecasts;
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return true;
        }
    }
}
