using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// BackgroundService bağımlılığını kaldırıyoruz
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MesTechStok.Core.Services.Security
{
    /// <summary>
    /// Token rotation ayarları
    /// </summary>
    public class TokenRotationSettings
    {
        public TimeSpan RotationInterval { get; set; } = TimeSpan.FromHours(8); // 8 saatte bir
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(24); // Token 24 saat geçerli
        public TimeSpan RefreshThreshold { get; set; } = TimeSpan.FromHours(2); // 2 saat kala yenile
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnablePreemptiveRotation { get; set; } = true;
        public string TokenStorePath { get; set; } = "tokens.json";
    }

    /// <summary>
    /// Token bilgileri modeli
    /// </summary>
    public class SecurityToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TokenType { get; set; } = string.Empty; // ApiKey, Bearer, Basic, etc.
        public string ServiceName { get; set; } = string.Empty; // OpenCart, WeatherAPI, etc.
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, string> Metadata { get; set; } = new();

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool NeedsRefresh(TimeSpan threshold) => DateTime.UtcNow >= ExpiresAt.Subtract(threshold);
        public TimeSpan TimeToExpiry => ExpiresAt - DateTime.UtcNow;
    }

    /// <summary>
    /// Token rotation sonuç modeli
    /// </summary>
    public class TokenRotationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public SecurityToken? NewToken { get; set; }
        public SecurityToken? OldToken { get; set; }
        public DateTime RotationTime { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public string Strategy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token provider interface - her servis için implement edilir
    /// </summary>
    public interface ITokenProvider
    {
        string ServiceName { get; }
        Task<SecurityToken> RefreshTokenAsync(SecurityToken currentToken, CancellationToken cancellationToken = default);
        Task<SecurityToken> GetNewTokenAsync(CancellationToken cancellationToken = default);
        Task<bool> ValidateTokenAsync(SecurityToken token, CancellationToken cancellationToken = default);
        Task RevokeTokenAsync(SecurityToken token, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Token storage interface
    /// </summary>
    public interface ITokenStorage
    {
        Task SaveTokenAsync(SecurityToken token);
        Task<SecurityToken?> GetTokenAsync(string serviceName, string? tokenId = null);
        Task<IEnumerable<SecurityToken>> GetAllTokensAsync();
        Task DeleteTokenAsync(string serviceName, string tokenId);
        Task<bool> UpdateTokenAsync(SecurityToken token);
    }

    /// <summary>
    /// Token rotation service interface
    /// </summary>
    public interface ITokenRotationService
    {
        Task<TokenRotationResult> RotateTokenAsync(string serviceName, bool forceRotation = false);
        Task<SecurityToken?> GetValidTokenAsync(string serviceName);
        Task<bool> IsTokenValidAsync(string serviceName);
        Task RotateAllTokensAsync();
        Task<IEnumerable<SecurityToken>> GetTokenStatusAsync();
        event EventHandler<TokenRotationResult>? TokenRotated;
    }

    /// <summary>
    /// File-based token storage implementasyonu
    /// </summary>
    public class FileTokenStorage : ITokenStorage
    {
        private readonly string _filePath;
        private readonly ILogger<FileTokenStorage> _logger;
        private readonly object _fileLock = new object();

        public FileTokenStorage(string filePath, ILogger<FileTokenStorage> logger)
        {
            _filePath = filePath;
            _logger = logger;
        }

        public async Task SaveTokenAsync(SecurityToken token)
        {
            try
            {
                var tokens = await LoadTokensAsync();
                tokens[GetTokenKey(token)] = token;
                await SaveTokensAsync(tokens);

                _logger.LogDebug("[TokenStorage] Saved token for {Service} (ID: {TokenId})",
                    token.ServiceName, token.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenStorage] Failed to save token for {Service}", token.ServiceName);
                throw;
            }
        }

        public async Task<SecurityToken?> GetTokenAsync(string serviceName, string? tokenId = null)
        {
            try
            {
                var tokens = await LoadTokensAsync();

                if (tokenId != null)
                {
                    var key = $"{serviceName}:{tokenId}";
                    return tokens.TryGetValue(key, out var token) ? token : null;
                }

                // Son aktif token'ı bul
                var serviceTokens = tokens.Values.Where(t => t.ServiceName == serviceName && t.IsActive);
                return serviceTokens.OrderByDescending(t => t.IssuedAt).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenStorage] Failed to get token for {Service}", serviceName);
                return null;
            }
        }

        public async Task<IEnumerable<SecurityToken>> GetAllTokensAsync()
        {
            try
            {
                var tokens = await LoadTokensAsync();
                return tokens.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenStorage] Failed to get all tokens");
                return Enumerable.Empty<SecurityToken>();
            }
        }

        public async Task DeleteTokenAsync(string serviceName, string tokenId)
        {
            try
            {
                var tokens = await LoadTokensAsync();
                var key = $"{serviceName}:{tokenId}";

                if (tokens.Remove(key))
                {
                    await SaveTokensAsync(tokens);
                    _logger.LogDebug("[TokenStorage] Deleted token {TokenId} for {Service}", tokenId, serviceName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenStorage] Failed to delete token {TokenId} for {Service}", tokenId, serviceName);
                throw;
            }
        }

        public async Task<bool> UpdateTokenAsync(SecurityToken token)
        {
            try
            {
                var tokens = await LoadTokensAsync();
                var key = GetTokenKey(token);

                if (tokens.ContainsKey(key))
                {
                    tokens[key] = token;
                    await SaveTokensAsync(tokens);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenStorage] Failed to update token for {Service}", token.ServiceName);
                return false;
            }
        }

        private async Task<Dictionary<string, SecurityToken>> LoadTokensAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, SecurityToken>();
            }

            lock (_fileLock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, SecurityToken>>(json)
                       ?? new Dictionary<string, SecurityToken>();
            }
        }

        private async Task SaveTokensAsync(Dictionary<string, SecurityToken> tokens)
        {
            var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });

            lock (_fileLock)
            {
                File.WriteAllText(_filePath, json);
            }
        }

        private string GetTokenKey(SecurityToken token) => $"{token.ServiceName}:{token.Id}";
    }

    /// <summary>
    /// Token rotation service implementasyonu
    /// Background service olarak çalışır ve token'ları otomatik yeniler
    /// </summary>
    public class TokenRotationService : ITokenRotationService, IDisposable
    {
        private readonly ITokenStorage _tokenStorage;
        private readonly IEnumerable<ITokenProvider> _tokenProviders;
        private readonly ILogger<TokenRotationService> _logger;
        private readonly TokenRotationSettings _settings;

        public event EventHandler<TokenRotationResult>? TokenRotated;
        private readonly System.Threading.Timer _timer;

        public TokenRotationService(
            ITokenStorage tokenStorage,
            IEnumerable<ITokenProvider> tokenProviders,
            ILogger<TokenRotationService> logger,
            IOptions<TokenRotationSettings> settings)
        {
            _tokenStorage = tokenStorage;
            _tokenProviders = tokenProviders;
            _logger = logger;
            _settings = settings.Value;
            _timer = new System.Threading.Timer(async _ => await SafeCheckAsync(), null, _settings.RotationInterval, _settings.RotationInterval);
        }

        private async Task SafeCheckAsync()
        {
            try
            {
                if (_settings.EnablePreemptiveRotation)
                {
                    await CheckAndRotateTokensAsync(CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TokenRotation] Timer loop error");
            }
        }

        /// <summary>
        /// Belirli bir servis için token rotation
        /// </summary>
        public async Task<TokenRotationResult> RotateTokenAsync(string serviceName, bool forceRotation = false)
        {
            var startTime = DateTime.UtcNow;
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            _logger.LogInformation("[TokenRotation] Starting rotation for {Service}. Force: {Force}, CorrelationId: {CorrelationId}",
                serviceName, forceRotation, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

            try
            {
                var provider = _tokenProviders.FirstOrDefault(p => p.ServiceName == serviceName);
                if (provider == null)
                {
                    var error = $"No token provider found for service: {serviceName}";
                    _logger.LogWarning("[TokenRotation] {Error}", error);
                    return new TokenRotationResult { Success = false, ErrorMessage = error };
                }

                var currentToken = await _tokenStorage.GetTokenAsync(serviceName);

                // Rotation gerekli mi kontrol et
                if (!forceRotation && currentToken != null && !currentToken.NeedsRefresh(_settings.RefreshThreshold))
                {
                    _logger.LogDebug("[TokenRotation] {Service} token is still valid, skipping rotation", serviceName);
                    return new TokenRotationResult
                    {
                        Success = true,
                        NewToken = currentToken,
                        Strategy = "Skipped"
                    };
                }

                SecurityToken newToken;
                string strategy;

                // Token yenileme stratejisi
                if (currentToken?.RefreshToken != null && !currentToken.IsExpired)
                {
                    // Refresh token kullan
                    newToken = await provider.RefreshTokenAsync(currentToken);
                    strategy = "Refresh";
                    _logger.LogDebug("[TokenRotation] {Service} token refreshed using refresh token", serviceName);
                }
                else
                {
                    // Yeni token al
                    newToken = await provider.GetNewTokenAsync();
                    strategy = "New";
                    _logger.LogDebug("[TokenRotation] {Service} obtained new token", serviceName);
                }

                // Yeni token'ı kaydet
                await _tokenStorage.SaveTokenAsync(newToken);

                // Eski token'ı revoke et ve deaktive et
                if (currentToken != null)
                {
                    try
                    {
                        await provider.RevokeTokenAsync(currentToken);
                        currentToken.IsActive = false;
                        await _tokenStorage.UpdateTokenAsync(currentToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[TokenRotation] Failed to revoke old token for {Service}", serviceName);
                    }
                }

                var result = new TokenRotationResult
                {
                    Success = true,
                    NewToken = newToken,
                    OldToken = currentToken,
                    Duration = DateTime.UtcNow - startTime,
                    Strategy = strategy
                };

                TokenRotated?.Invoke(this, result);

                _logger.LogInformation("[TokenRotation] Successfully rotated token for {Service}. Strategy: {Strategy}, Duration: {Duration}ms",
                    serviceName, strategy, result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                var result = new TokenRotationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = DateTime.UtcNow - startTime,
                    Strategy = "Error"
                };

                _logger.LogError(ex, "[TokenRotation] Failed to rotate token for {Service}", serviceName);
                return result;
            }
        }

        /// <summary>
        /// Geçerli token getirir, gerekirse yeniler
        /// </summary>
        public async Task<SecurityToken?> GetValidTokenAsync(string serviceName)
        {
            var token = await _tokenStorage.GetTokenAsync(serviceName);

            if (token == null || token.IsExpired)
            {
                var rotationResult = await RotateTokenAsync(serviceName, forceRotation: true);
                return rotationResult.Success ? rotationResult.NewToken : null;
            }

            if (token.NeedsRefresh(_settings.RefreshThreshold))
            {
                // Background'da yenile
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RotateTokenAsync(serviceName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[TokenRotation] Background refresh failed for {Service}", serviceName);
                    }
                });
            }

            token.LastUsed = DateTime.UtcNow;
            await _tokenStorage.UpdateTokenAsync(token);

            return token;
        }

        /// <summary>
        /// Token geçerliliğini kontrol eder
        /// </summary>
        public async Task<bool> IsTokenValidAsync(string serviceName)
        {
            var token = await GetValidTokenAsync(serviceName);
            return token != null && !token.IsExpired;
        }

        /// <summary>
        /// Tüm token'ları yeniler
        /// </summary>
        public async Task RotateAllTokensAsync()
        {
            _logger.LogInformation("[TokenRotation] Starting rotation for all services");

            var tasks = _tokenProviders.Select(async provider =>
            {
                try
                {
                    await RotateTokenAsync(provider.ServiceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TokenRotation] Failed to rotate token for {Service}", provider.ServiceName);
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("[TokenRotation] Completed rotation for all services");
        }

        /// <summary>
        /// Tüm token durumlarını getirir
        /// </summary>
        public async Task<IEnumerable<SecurityToken>> GetTokenStatusAsync()
        {
            return await _tokenStorage.GetAllTokensAsync();
        }

        public void Dispose()
        {
            try { _timer?.Dispose(); } catch { }
        }

        /// <summary>
        /// Token'ları kontrol eder ve gerekirse yeniler
        /// </summary>
        private async Task CheckAndRotateTokensAsync(CancellationToken cancellationToken)
        {
            var allTokens = await _tokenStorage.GetAllTokensAsync();
            var activeTokens = allTokens.Where(t => t.IsActive && !t.IsExpired).ToList();

            _logger.LogDebug("[TokenRotation] Checking {Count} active tokens", activeTokens.Count);

            foreach (var token in activeTokens)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (token.NeedsRefresh(_settings.RefreshThreshold))
                    {
                        _logger.LogDebug("[TokenRotation] Token for {Service} needs refresh (expires in {TimeToExpiry})",
                            token.ServiceName, token.TimeToExpiry);

                        await RotateTokenAsync(token.ServiceName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TokenRotation] Failed to check/rotate token for {Service}", token.ServiceName);
                }
            }
        }
    }

    /// <summary>
    /// OpenCart token provider örneği
    /// </summary>
    public class OpenCartTokenProvider : ITokenProvider
    {
        public string ServiceName => "OpenCart";
        private readonly ILogger<OpenCartTokenProvider> _logger;

        public OpenCartTokenProvider(ILogger<OpenCartTokenProvider> logger)
        {
            _logger = logger;
        }

        public async Task<SecurityToken> RefreshTokenAsync(SecurityToken currentToken, CancellationToken cancellationToken = default)
        {
            // TODO: OpenCart API refresh token logic
            await Task.Delay(100, cancellationToken);

            return new SecurityToken
            {
                ServiceName = ServiceName,
                TokenType = "ApiKey",
                AccessToken = $"refreshed_token_{DateTime.UtcNow.Ticks}",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<SecurityToken> GetNewTokenAsync(CancellationToken cancellationToken = default)
        {
            // TODO: OpenCart API new token logic
            await Task.Delay(100, cancellationToken);

            return new SecurityToken
            {
                ServiceName = ServiceName,
                TokenType = "ApiKey",
                AccessToken = $"new_token_{DateTime.UtcNow.Ticks}",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<bool> ValidateTokenAsync(SecurityToken token, CancellationToken cancellationToken = default)
        {
            // TODO: OpenCart API token validation logic
            await Task.Delay(50, cancellationToken);
            return !token.IsExpired;
        }

        public async Task RevokeTokenAsync(SecurityToken token, CancellationToken cancellationToken = default)
        {
            // TODO: OpenCart API token revocation logic
            await Task.Delay(50, cancellationToken);
            _logger.LogDebug("[OpenCartTokenProvider] Revoked token {TokenId}", token.Id);
        }
    }
}
