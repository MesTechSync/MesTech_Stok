using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using System.Text;
using System.Security.Cryptography;

namespace MesTechStok.Core.Services
{
    /// <summary>
    /// AI Configuration Service - Enterprise Grade AI Management
    /// A++++ kalitede AI API yönetim servisi
    /// </summary>
    public interface IAIConfigurationService
    {
        Task<List<AIConfiguration>> GetAllConfigurationsAsync();
        Task<AIConfiguration?> GetConfigurationAsync(int id);
        Task<AIConfiguration?> GetDefaultConfigurationAsync();
        Task<AIConfiguration> SaveConfigurationAsync(AIConfiguration config);
        Task<bool> DeleteConfigurationAsync(int id);
        Task<bool> TestConnectionAsync(int configId);
        Task<AIConfiguration> SetAsDefaultAsync(int configId);
        Task<Dictionary<string, object>> GetUsageStatisticsAsync();
        Task<bool> IsWithinRateLimitsAsync(int configId);
        Task LogUsageAsync(int configId, string requestType, int tokens, int responseTime, decimal cost, bool isSuccessful, string? error = null);
        Task<string> CallAIServiceAsync(int configId, string prompt, string requestType);
        Task ResetDailyCountersAsync();
    }

    public class AIConfigurationService : IAIConfigurationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AIConfigurationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _encryptionKey;

        public AIConfigurationService(AppDbContext context, ILogger<AIConfigurationService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _encryptionKey = "MesTech_AI_2025_Secure_Key_v2.0"; // Production'da güvenli key yönetimi
        }

        public async Task<List<AIConfiguration>> GetAllConfigurationsAsync()
        {
            try
            {
                _logger.LogInformation("[AI_CONFIG] Fetching all AI configurations");
                var configs = await _context.AIConfigurations
                    .OrderBy(c => c.ProviderName)
                    .ToListAsync();

                // Decrypt API keys for display (masking)
                foreach (var config in configs)
                {
                    config.ApiKey = MaskApiKey(config.ApiKey);
                }

                _logger.LogInformation("[AI_CONFIG] Successfully loaded {Count} AI configurations", configs.Count);
                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error fetching AI configurations");
                return new List<AIConfiguration>();
            }
        }

        public async Task<AIConfiguration?> GetConfigurationAsync(int id)
        {
            try
            {
                var config = await _context.AIConfigurations
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (config != null)
                {
                    _logger.LogInformation("[AI_CONFIG] Configuration loaded for ID: {Id}, Provider: {Provider}", id, config.ProviderName);
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error fetching configuration with ID: {Id}", id);
                return null;
            }
        }

        public async Task<AIConfiguration?> GetDefaultConfigurationAsync()
        {
            try
            {
                var defaultConfig = await _context.AIConfigurations
                    .Where(c => c.IsDefault && c.IsActive)
                    .FirstOrDefaultAsync();

                if (defaultConfig == null)
                {
                    defaultConfig = await _context.AIConfigurations
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.ProviderName)
                        .FirstOrDefaultAsync();
                }

                if (defaultConfig != null)
                {
                    _logger.LogInformation("[AI_CONFIG] Default configuration: {Provider}", defaultConfig.ProviderName);
                }

                return defaultConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error fetching default configuration");
                return null;
            }
        }

        public async Task<AIConfiguration> SaveConfigurationAsync(AIConfiguration config)
        {
            try
            {
                _logger.LogInformation("[AI_CONFIG] Saving configuration for provider: {Provider}", config.ProviderName);

                // Validation
                if (string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.ProviderName))
                {
                    throw new ArgumentException("API Key ve Provider Name zorunludur");
                }

                // Encrypt API key before saving
                if (!IsApiKeyMasked(config.ApiKey))
                {
                    config.ApiKey = EncryptApiKey(config.ApiKey);
                }

                if (config.Id == 0)
                {
                    config.CreatedDate = DateTime.Now;
                    _context.AIConfigurations.Add(config);
                    _logger.LogInformation("[AI_CONFIG] Creating new configuration");
                }
                else
                {
                    config.ModifiedDate = DateTime.Now;
                    _context.AIConfigurations.Update(config);
                    _logger.LogInformation("[AI_CONFIG] Updating configuration ID: {Id}", config.Id);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("[AI_CONFIG] Configuration saved successfully with ID: {Id}", config.Id);

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error saving configuration for provider: {Provider}", config.ProviderName);
                throw;
            }
        }

        public async Task<bool> DeleteConfigurationAsync(int id)
        {
            try
            {
                var config = await _context.AIConfigurations.FindAsync(id);
                if (config == null)
                {
                    _logger.LogWarning("[AI_CONFIG] Configuration not found for deletion: ID {Id}", id);
                    return false;
                }

                _context.AIConfigurations.Remove(config);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[AI_CONFIG] Configuration deleted: ID {Id}, Provider: {Provider}", id, config.ProviderName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error deleting configuration ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(int configId)
        {
            try
            {
                var config = await GetConfigurationAsync(configId);
                if (config == null)
                {
                    _logger.LogWarning("[AI_CONFIG] Configuration not found for testing: ID {Id}", configId);
                    return false;
                }

                _logger.LogInformation("[AI_CONFIG] Testing connection for provider: {Provider}", config.ProviderName);

                var decryptedApiKey = DecryptApiKey(config.ApiKey);
                var testResult = await PerformConnectionTestAsync(config, decryptedApiKey);

                if (testResult)
                {
                    config.MarkHealthy();
                    _logger.LogInformation("[AI_CONFIG] Connection test successful for {Provider}", config.ProviderName);
                }
                else
                {
                    config.LogError("Connection test failed");
                    _logger.LogWarning("[AI_CONFIG] Connection test failed for {Provider}", config.ProviderName);
                }

                await _context.SaveChangesAsync();
                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error testing connection for config ID: {Id}", configId);
                return false;
            }
        }

        public async Task<AIConfiguration> SetAsDefaultAsync(int configId)
        {
            try
            {
                // Reset all as non-default
                var allConfigs = await _context.AIConfigurations.ToListAsync();
                foreach (var c in allConfigs)
                {
                    c.IsDefault = false;
                }

                // Set the specified as default
                var targetConfig = allConfigs.FirstOrDefault(c => c.Id == configId);
                if (targetConfig == null)
                {
                    throw new ArgumentException($"Configuration with ID {configId} not found");
                }

                targetConfig.IsDefault = true;
                targetConfig.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("[AI_CONFIG] Configuration set as default: ID {Id}, Provider: {Provider}", configId, targetConfig.ProviderName);
                return targetConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error setting default configuration: ID {Id}", configId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetUsageStatisticsAsync()
        {
            try
            {
                var stats = new Dictionary<string, object>();

                var configs = await _context.AIConfigurations.ToListAsync();
                var logs = await _context.AIUsageLogs
                    .Where(l => l.RequestDate >= DateTime.Today.AddDays(-30))
                    .ToListAsync();

                stats["TotalProviders"] = configs.Count;
                stats["ActiveProviders"] = configs.Count(c => c.IsActive);
                stats["TotalDailyRequests"] = configs.Sum(c => c.DailyRequestCount);
                stats["TotalMonthlyRequests"] = configs.Sum(c => c.MonthlyRequestCount);
                stats["TotalCost"] = configs.Sum(c => c.TotalCost);
                stats["SuccessfulRequests"] = logs.Count(l => l.IsSuccessful);
                stats["FailedRequests"] = logs.Count(l => !l.IsSuccessful);
                stats["AverageResponseTime"] = logs.Any() ? logs.Average(l => l.ResponseTimeMs) : 0;

                var providerStats = configs.Select(c => new
                {
                    Provider = c.ProviderName,
                    DailyRequests = c.DailyRequestCount,
                    MonthlyRequests = c.MonthlyRequestCount,
                    Cost = c.TotalCost,
                    IsHealthy = c.IsHealthy,
                    UsagePercentage = c.MaxRequestsPerDay > 0 ? (c.DailyRequestCount * 100.0 / c.MaxRequestsPerDay) : 0
                }).ToList();

                stats["ProviderDetails"] = providerStats;

                _logger.LogInformation("[AI_CONFIG] Usage statistics compiled successfully");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error compiling usage statistics");
                return new Dictionary<string, object>();
            }
        }

        public async Task<bool> IsWithinRateLimitsAsync(int configId)
        {
            try
            {
                var config = await GetConfigurationAsync(configId);
                return config?.IsWithinRateLimit() ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error checking rate limits for config ID: {Id}", configId);
                return false;
            }
        }

        public async Task LogUsageAsync(int configId, string requestType, int tokens, int responseTime, decimal cost, bool isSuccessful, string? error = null)
        {
            try
            {
                var config = await GetConfigurationAsync(configId);
                if (config == null) return;

                // Update configuration usage
                config.IncrementUsage();
                config.TotalCost += cost;

                if (!isSuccessful && !string.IsNullOrEmpty(error))
                {
                    config.LogError(error);
                }

                // Create usage log
                var log = new AIUsageLog
                {
                    AIConfigurationId = configId,
                    RequestType = requestType,
                    TokensUsed = tokens,
                    ResponseTimeMs = responseTime,
                    Cost = cost,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = error,
                    RequestDate = DateTime.Now
                };

                _context.AIUsageLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[AI_CONFIG] Usage logged: Provider {Provider}, Type {RequestType}, Success {IsSuccessful}",
                    config.ProviderName, requestType, isSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error logging usage for config ID: {Id}", configId);
            }
        }

        public async Task<string> CallAIServiceAsync(int configId, string prompt, string requestType)
        {
            var startTime = DateTime.Now;
            try
            {
                var config = await GetConfigurationAsync(configId);
                if (config == null)
                {
                    throw new ArgumentException($"AI Configuration with ID {configId} not found");
                }

                if (!config.IsWithinRateLimit())
                {
                    throw new InvalidOperationException($"Rate limit exceeded for {config.ProviderName}");
                }

                var decryptedApiKey = DecryptApiKey(config.ApiKey);
                var response = await CallProviderAPIAsync(config, decryptedApiKey, prompt);
                var responseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;

                await LogUsageAsync(configId, requestType, EstimateTokens(prompt), responseTime, 0.01m, true);

                _logger.LogInformation("[AI_CONFIG] AI service call successful: {Provider}, Response time: {ResponseTime}ms",
                    config.ProviderName, responseTime);

                return response;
            }
            catch (Exception ex)
            {
                var responseTime = (int)(DateTime.Now - startTime).TotalMilliseconds;
                await LogUsageAsync(configId, requestType, EstimateTokens(prompt), responseTime, 0, false, ex.Message);

                _logger.LogError(ex, "[AI_CONFIG] AI service call failed for config ID: {Id}", configId);
                throw;
            }
        }

        public async Task ResetDailyCountersAsync()
        {
            try
            {
                var configs = await _context.AIConfigurations.ToListAsync();
                foreach (var config in configs)
                {
                    config.DailyRequestCount = 0;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("[AI_CONFIG] Daily counters reset for {Count} configurations", configs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_CONFIG] Error resetting daily counters");
            }
        }

        #region Private Helper Methods

        private string EncryptApiKey(string apiKey)
        {
            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey.Substring(0, 32));
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(apiKey);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                var result = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch
            {
                return apiKey; // Fallback: return as is
            }
        }

        private string DecryptApiKey(string encryptedApiKey)
        {
            try
            {
                if (IsApiKeyMasked(encryptedApiKey))
                {
                    throw new InvalidOperationException("Cannot decrypt masked API key");
                }

                var keyBytes = Encoding.UTF8.GetBytes(_encryptionKey.Substring(0, 32));
                var encryptedBytes = Convert.FromBase64String(encryptedApiKey);

                using var aes = Aes.Create();
                aes.Key = keyBytes;

                var iv = new byte[aes.IV.Length];
                var encrypted = new byte[encryptedBytes.Length - iv.Length];

                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, iv.Length, encrypted, 0, encrypted.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return encryptedApiKey; // Fallback: return as is
            }
        }

        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 8)
                return "••••••••";

            return apiKey.Substring(0, 4) + "••••••••" + apiKey.Substring(apiKey.Length - 4);
        }

        private bool IsApiKeyMasked(string apiKey)
        {
            return apiKey.Contains("••••");
        }

        private async Task<bool> PerformConnectionTestAsync(AIConfiguration config, string apiKey)
        {
            try
            {
                var testPrompt = "Test connection - respond with 'OK'";
                var response = await CallProviderAPIAsync(config, apiKey, testPrompt);
                return !string.IsNullOrWhiteSpace(response);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> CallProviderAPIAsync(AIConfiguration config, string apiKey, string prompt)
        {
            var timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            _httpClient.Timeout = timeout;

            switch (config.ProviderName.ToLower())
            {
                case "chatgpt":
                    return await CallOpenAIAsync(config, apiKey, prompt);
                case "gemini":
                    return await CallGeminiAsync(config, apiKey, prompt);
                case "deepseek":
                    return await CallDeepSeekAsync(config, apiKey, prompt);
                case "claude":
                    return await CallClaudeAsync(config, apiKey, prompt);
                default:
                    throw new NotSupportedException($"Provider {config.ProviderName} is not supported");
            }
        }

        private async Task<string> CallOpenAIAsync(AIConfiguration config, string apiKey, string prompt)
        {
            var request = new
            {
                model = config.ModelName ?? "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = config.SystemPrompt ?? "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                max_tokens = config.MaxTokens,
                temperature = config.Temperature
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(config.ApiEndpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        private async Task<string> CallGeminiAsync(AIConfiguration config, string apiKey, string prompt)
        {
            // Simplified Gemini implementation
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var endpoint = $"{config.ApiEndpoint}?key={apiKey}";
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }

        private async Task<string> CallDeepSeekAsync(AIConfiguration config, string apiKey, string prompt)
        {
            var request = new
            {
                model = config.ModelName ?? "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = config.SystemPrompt ?? "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                max_tokens = config.MaxTokens,
                temperature = config.Temperature
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(config.ApiEndpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"DeepSeek API error: {response.StatusCode} - {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        private async Task<string> CallClaudeAsync(AIConfiguration config, string apiKey, string prompt)
        {
            var request = new
            {
                model = config.ModelName ?? "claude-3-sonnet-20240229",
                max_tokens = config.MaxTokens,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(config.ApiEndpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Claude API error: {response.StatusCode} - {responseJson}");
            }

            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        }

        private int EstimateTokens(string text)
        {
            // Rough estimation: 1 token ≈ 4 characters for most languages
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        #endregion
    }
}
