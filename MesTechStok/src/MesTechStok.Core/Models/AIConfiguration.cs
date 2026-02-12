using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// AI API Configuration Model - A++++ Quality Enterprise Integration
    /// AI API ayarları ve kullanım metrikleri için veri modeli
    /// </summary>
    [Table("AIConfigurations")]
    public class AIConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ProviderName { get; set; } = string.Empty; // "ChatGPT", "Gemini", "DeepSeek", "Claude"

        [Required]
        [StringLength(500)]
        public string ApiKey { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ApiEndpoint { get; set; }

        [StringLength(100)]
        public string? ModelName { get; set; }

        [StringLength(100)]
        public string? Model { get; set; } // Added for UI compatibility

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; } = false;

        // Usage Metrics
        public int DailyRequestCount { get; set; } = 0;
        public int MonthlyRequestCount { get; set; } = 0;
        public decimal TotalCost { get; set; } = 0;

        // Rate Limiting
        public int MaxRequestsPerMinute { get; set; } = 60;
        public int MaxRequestsPerDay { get; set; } = 1000;
        public int? DailyLimit { get; set; } = 1000; // Added for UI compatibility

        // Quality & Performance Settings
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
        public int TimeoutSeconds { get; set; } = 30;

        // Professional Settings
        public string? SystemPrompt { get; set; }
        public bool EnableLogging { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;

        // Security
        [StringLength(100)]
        public string? EncryptionKey { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }

        // Status Information
        [StringLength(500)]
        public string? LastErrorMessage { get; set; }
        public DateTime? LastErrorDate { get; set; }
        public bool IsHealthy { get; set; } = true;

        // Provider-Specific Settings (JSON)
        [Column(TypeName = "nvarchar(max)")]
        public string? ProviderSettings { get; set; }

        // Business Logic Methods
        public void IncrementUsage()
        {
            DailyRequestCount++;
            MonthlyRequestCount++;
            LastUsedDate = DateTime.Now;
        }

        public void LogError(string errorMessage)
        {
            LastErrorMessage = errorMessage;
            LastErrorDate = DateTime.Now;
            IsHealthy = false;
        }

        public void MarkHealthy()
        {
            LastErrorMessage = null;
            LastErrorDate = null;
            IsHealthy = true;
        }

        public bool IsWithinRateLimit()
        {
            return DailyRequestCount < MaxRequestsPerDay;
        }

        public bool IsConfigurationValid()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) &&
                   !string.IsNullOrWhiteSpace(ProviderName) &&
                   IsActive;
        }

        public string GetDisplayName()
        {
            return $"{ProviderName} ({(IsActive ? "Aktif" : "Devre Dışı")})";
        }

        public string GetUsageStatus()
        {
            var percentage = MaxRequestsPerDay > 0 ? (DailyRequestCount * 100.0 / MaxRequestsPerDay) : 0;
            return $"{DailyRequestCount}/{MaxRequestsPerDay} ({percentage:F1}%)";
        }
    }

    /// <summary>
    /// AI Usage Log Model - Professional Monitoring
    /// AI kullanım logları ve metrikler için detaylı kayıt
    /// </summary>
    [Table("AIUsageLogs")]
    public class AIUsageLog
    {
        [Key]
        public int Id { get; set; }

        public int AIConfigurationId { get; set; }

        [ForeignKey("AIConfigurationId")]
        public virtual AIConfiguration AIConfiguration { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string RequestType { get; set; } = string.Empty; // "ProductAnalysis", "PriceOptimization", etc.

        public int TokensUsed { get; set; }
        public int ResponseTimeMs { get; set; }
        public decimal Cost { get; set; }

        public bool IsSuccessful { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? RequestData { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ResponseData { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UserId { get; set; }

        [StringLength(200)]
        public string? IpAddress { get; set; }

        // Performance Analysis
        public double? ConfidenceScore { get; set; }
        public int? QualityRating { get; set; } // 1-5 scale

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// AI Provider Template - Standard Configurations
    /// AI sağlayıcıları için öntanımlı konfigürasyonlar
    /// </summary>
    public static class AIProviderTemplates
    {
        public static AIConfiguration CreateChatGPTConfiguration(string apiKey)
        {
            return new AIConfiguration
            {
                ProviderName = "ChatGPT",
                ApiKey = apiKey,
                ApiEndpoint = "https://api.openai.com/v1/chat/completions",
                ModelName = "gpt-4o",
                MaxRequestsPerMinute = 50,
                MaxRequestsPerDay = 1000,
                Temperature = 0.7,
                MaxTokens = 2000,
                TimeoutSeconds = 30,
                SystemPrompt = "Sen MesTech Stok Takip Sistemi için uzman bir AI asistanısın. Profesyonel, doğru ve faydalı yanıtlar ver.",
                EnableLogging = true,
                EnableMetrics = true
            };
        }

        public static AIConfiguration CreateGeminiConfiguration(string apiKey)
        {
            return new AIConfiguration
            {
                ProviderName = "Gemini",
                ApiKey = apiKey,
                ApiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent",
                ModelName = "gemini-pro",
                MaxRequestsPerMinute = 60,
                MaxRequestsPerDay = 1500,
                Temperature = 0.7,
                MaxTokens = 1500,
                TimeoutSeconds = 25,
                SystemPrompt = "MesTech sisteminde stok, ürün ve iş süreçleri konularında uzman yardımcısın.",
                EnableLogging = true,
                EnableMetrics = true
            };
        }

        public static AIConfiguration CreateDeepSeekConfiguration(string apiKey)
        {
            return new AIConfiguration
            {
                ProviderName = "DeepSeek",
                ApiKey = apiKey,
                ApiEndpoint = "https://api.deepseek.com/v1/chat/completions",
                ModelName = "deepseek-chat",
                MaxRequestsPerMinute = 40,
                MaxRequestsPerDay = 800,
                Temperature = 0.6,
                MaxTokens = 1200,
                TimeoutSeconds = 35,
                SystemPrompt = "Stok yönetimi ve iş zekası konularında derinlemesine analiz yapan uzman asistansın.",
                EnableLogging = true,
                EnableMetrics = true
            };
        }

        public static AIConfiguration CreateClaudeConfiguration(string apiKey)
        {
            return new AIConfiguration
            {
                ProviderName = "Claude",
                ApiKey = apiKey,
                ApiEndpoint = "https://api.anthropic.com/v1/messages",
                ModelName = "claude-3-sonnet-20240229",
                MaxRequestsPerMinute = 30,
                MaxRequestsPerDay = 600,
                Temperature = 0.7,
                MaxTokens = 1800,
                TimeoutSeconds = 40,
                SystemPrompt = "MesTech Stok Takip Sistemi için analitik ve stratejik düşünce odaklı AI uzmanısın.",
                EnableLogging = true,
                EnableMetrics = true
            };
        }
    }
}
