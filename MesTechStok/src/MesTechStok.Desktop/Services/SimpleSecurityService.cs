using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Basit güvenlik sistemi - Mevcut Core User modeli ile uyumlu
    /// </summary>
    public class SimpleSecurityService
    {
        private readonly ILogger<SimpleSecurityService> _logger;
        private static readonly Dictionary<string, UserSession> _activeSessions = new();
        private static readonly object _lockObject = new object();

        public SimpleSecurityService(ILogger<SimpleSecurityService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Basit kimlik doğrulama kontrolü: Herhangi bir aktif session var mı?
        /// </summary>
        public Task<bool> IsAuthenticatedAsync()
        {
            lock (_lockObject)
            {
                return Task.FromResult(_activeSessions.Count > 0);
            }
        }

        /// <summary>
        /// Basit kullanıcı girişi
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", username);

                // Basit kullanıcı doğrulama (demo amaçlı)
                if (IsValidUser(username, password))
                {
                    var sessionId = Guid.NewGuid().ToString();
                    var session = new UserSession
                    {
                        Username = username,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(8),
                        IsActive = true
                    };

                    lock (_lockObject)
                    {
                        _activeSessions[sessionId] = session;
                    }

                    _logger.LogInformation("Login successful for user: {Username}", username);

                    return new LoginResult
                    {
                        IsSuccess = true,
                        SessionId = sessionId,
                        Username = username,
                        ExpiresAt = session.ExpiresAt
                    };
                }

                _logger.LogWarning("Login failed for username: {Username}", username);
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Geçersiz kullanıcı adı veya şifre"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", username);
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Giriş sırasında bir hata oluştu"
                };
            }
        }

        /// <summary>
        /// Kullanıcı çıkışı
        /// </summary>
        public async Task<bool> LogoutAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeSessions.ContainsKey(sessionId))
                    {
                        _activeSessions.Remove(sessionId);
                        _logger.LogInformation("User logged out successfully - SessionId: {SessionId}", sessionId);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for session: {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// Session doğrulama
        /// </summary>
        public async Task<bool> ValidateSessionAsync(string sessionId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_activeSessions.TryGetValue(sessionId, out var session))
                    {
                        if (session.IsActive && session.ExpiresAt > DateTime.UtcNow)
                        {
                            // Session'ı uzat
                            session.ExpiresAt = DateTime.UtcNow.AddHours(8);
                            return true;
                        }

                        // Session süresi dolmuş, kaldır
                        _activeSessions.Remove(sessionId);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session: {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// Aktif session sayısı
        /// </summary>
        public int GetActiveSessionCount()
        {
            lock (_lockObject)
            {
                return _activeSessions.Count;
            }
        }

        /// <summary>
        /// Tüm session'ları temizle
        /// </summary>
        public void ClearAllSessions()
        {
            lock (_lockObject)
            {
                _activeSessions.Clear();
                _logger.LogInformation("All sessions cleared");
            }
        }

        /// <summary>
        /// Basit kullanıcı doğrulama (demo)
        /// </summary>
        private bool IsValidUser(string username, string password)
        {
            // Demo kullanıcılar + Database admin user
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "Admin123!" },  // Database ile uyumlu şifre
                { "user", "user123" },
                { "demo", "demo123" }
            };

            return validUsers.TryGetValue(username, out var validPassword) &&
                   validPassword == password;
        }
    }

    /// <summary>
    /// Kullanıcı session bilgisi
    /// </summary>
    public class UserSession
    {
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Giriş sonucu
    /// </summary>
    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SessionId { get; set; }
        public string? Username { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
