using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MesTechStok.Core.Services.Configuration
{
    /// <summary>
    /// Configuration değişiklik event modeli
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Configuration metadata
    /// </summary>
    public class ConfigurationMetadata
    {
        public string Key { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; } = "General";
        public bool IsRequired { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsReadOnly { get; set; }
        public string[]? ValidValues { get; set; }
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Configuration entry modeli
    /// </summary>
    public class ConfigurationEntry
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public ConfigurationMetadata Metadata { get; set; } = new();
        public string Source { get; set; } = "Default";
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public int AccessCount { get; set; }
    }

    /// <summary>
    /// Configuration validation sonucu
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> ValidatedValues { get; set; } = new();
    }

    /// <summary>
    /// Configuration backup modeli
    /// </summary>
    public class ConfigurationBackup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, object> Configurations { get; set; } = new();
        public string Version { get; set; } = "1.0";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Configuration source enum
    /// </summary>
    public enum ConfigurationSourceType
    {
        File,
        Database,
        EnvironmentVariable,
        CommandLine,
        InMemory,
        Remote,
        Encrypted
    }

    /// <summary>
    /// Configuration manager interface
    /// </summary>
    public interface IAdvancedConfigurationManager
    {
        // Basic operations
        Task<T?> GetValueAsync<T>(string key, T? defaultValue = default);
        Task SetValueAsync<T>(string key, T value, string? userId = null, string? reason = null);
        Task<bool> RemoveAsync(string key, string? userId = null, string? reason = null);
        Task<bool> ExistsAsync(string key);

        // Bulk operations
        Task<Dictionary<string, object>> GetAllAsync(string? category = null);
        Task SetMultipleAsync(Dictionary<string, object> values, string? userId = null, string? reason = null);
        Task<ConfigurationValidationResult> ValidateAsync(Dictionary<string, object> values);

        // Metadata operations
        Task<ConfigurationMetadata?> GetMetadataAsync(string key);
        Task SetMetadataAsync(string key, ConfigurationMetadata metadata);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<IEnumerable<ConfigurationEntry>> GetByCategoryAsync(string category);

        // Backup & Restore
        Task<ConfigurationBackup> CreateBackupAsync(string? description = null, string? userId = null);
        Task RestoreFromBackupAsync(string backupId, string? userId = null);
        Task<IEnumerable<ConfigurationBackup>> GetBackupsAsync();
        Task DeleteBackupAsync(string backupId);

        // Monitoring
        event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
        Task<IEnumerable<ConfigurationChangedEventArgs>> GetChangeHistoryAsync(string? key = null, DateTime? since = null);

        // Encryption
        Task<string> EncryptValueAsync(string value);
        Task<string> DecryptValueAsync(string encryptedValue);
        Task EncryptConfigurationAsync(string key);
        Task DecryptConfigurationAsync(string key);

        // Export/Import
        Task<string> ExportToJsonAsync(string? category = null);
        Task ImportFromJsonAsync(string json, bool overwriteExisting = false, string? userId = null);
        Task<byte[]> ExportToBinaryAsync();
        Task ImportFromBinaryAsync(byte[] data, string? userId = null);

        // Hot reload
        Task ReloadAsync();
        Task<bool> IsConfigurationChangedAsync();
    }

    /// <summary>
    /// Configuration persistence interface
    /// </summary>
    public interface IConfigurationPersistence
    {
        Task<ConfigurationEntry?> LoadAsync(string key);
        Task SaveAsync(ConfigurationEntry entry);
        Task<bool> DeleteAsync(string key);
        Task<IEnumerable<ConfigurationEntry>> LoadAllAsync();
        Task SaveAllAsync(IEnumerable<ConfigurationEntry> entries);
        Task<IEnumerable<ConfigurationChangedEventArgs>> GetChangeLogAsync(string? key = null, DateTime? since = null);
        Task LogChangeAsync(ConfigurationChangedEventArgs changeEvent);
        Task<ConfigurationBackup?> LoadBackupAsync(string backupId);
        Task SaveBackupAsync(ConfigurationBackup backup);
        Task<IEnumerable<ConfigurationBackup>> LoadBackupsAsync();
        Task DeleteBackupAsync(string backupId);
    }

    /// <summary>
    /// File-based configuration persistence
    /// </summary>
    public class FileConfigurationPersistence : IConfigurationPersistence
    {
        private readonly string _basePath;
        private readonly ILogger<FileConfigurationPersistence> _logger;
        private readonly object _fileLock = new object();

        public FileConfigurationPersistence(string basePath, ILogger<FileConfigurationPersistence> logger)
        {
            _basePath = basePath;
            _logger = logger;

            // Dizinleri oluştur
            Directory.CreateDirectory(Path.Combine(_basePath, "configs"));
            Directory.CreateDirectory(Path.Combine(_basePath, "backups"));
            Directory.CreateDirectory(Path.Combine(_basePath, "logs"));
        }

        public async Task<ConfigurationEntry?> LoadAsync(string key)
        {
            try
            {
                var filePath = GetConfigFilePath(key);

                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                var entry = JsonSerializer.Deserialize<ConfigurationEntry>(json);

                if (entry != null)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    entry.AccessCount++;
                    await SaveAsync(entry); // Access count güncelle
                }

                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to load configuration: {Key}", key);
                return null;
            }
        }

        public async Task SaveAsync(ConfigurationEntry entry)
        {
            try
            {
                var filePath = GetConfigFilePath(entry.Key);
                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });

                lock (_fileLock)
                {
                    File.WriteAllText(filePath, json);
                }

                _logger.LogDebug("[ConfigPersistence] Saved configuration: {Key}", entry.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to save configuration: {Key}", entry.Key);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                var filePath = GetConfigFilePath(key);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug("[ConfigPersistence] Deleted configuration: {Key}", key);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to delete configuration: {Key}", key);
                return false;
            }
        }

        public async Task<IEnumerable<ConfigurationEntry>> LoadAllAsync()
        {
            try
            {
                var configsPath = Path.Combine(_basePath, "configs");
                var entries = new List<ConfigurationEntry>();

                if (!Directory.Exists(configsPath))
                    return entries;

                var files = Directory.GetFiles(configsPath, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var entry = JsonSerializer.Deserialize<ConfigurationEntry>(json);

                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[ConfigPersistence] Failed to load config file: {File}", file);
                    }
                }

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to load all configurations");
                return Enumerable.Empty<ConfigurationEntry>();
            }
        }

        public async Task SaveAllAsync(IEnumerable<ConfigurationEntry> entries)
        {
            foreach (var entry in entries)
            {
                await SaveAsync(entry);
            }
        }

        public async Task<IEnumerable<ConfigurationChangedEventArgs>> GetChangeLogAsync(string? key = null, DateTime? since = null)
        {
            try
            {
                var logPath = Path.Combine(_basePath, "logs", "changes.json");

                if (!File.Exists(logPath))
                    return Enumerable.Empty<ConfigurationChangedEventArgs>();

                var json = await File.ReadAllTextAsync(logPath);
                var allChanges = JsonSerializer.Deserialize<List<ConfigurationChangedEventArgs>>(json)
                                ?? new List<ConfigurationChangedEventArgs>();

                var filteredChanges = allChanges.AsEnumerable();

                if (!string.IsNullOrEmpty(key))
                {
                    filteredChanges = filteredChanges.Where(c => c.Key == key);
                }

                if (since.HasValue)
                {
                    filteredChanges = filteredChanges.Where(c => c.Timestamp >= since.Value);
                }

                return filteredChanges.OrderByDescending(c => c.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to get change log");
                return Enumerable.Empty<ConfigurationChangedEventArgs>();
            }
        }

        public async Task LogChangeAsync(ConfigurationChangedEventArgs changeEvent)
        {
            try
            {
                var logPath = Path.Combine(_basePath, "logs", "changes.json");
                var changes = new List<ConfigurationChangedEventArgs>();

                if (File.Exists(logPath))
                {
                    var json = await File.ReadAllTextAsync(logPath);
                    changes = JsonSerializer.Deserialize<List<ConfigurationChangedEventArgs>>(json)
                             ?? new List<ConfigurationChangedEventArgs>();
                }

                changes.Add(changeEvent);

                // Son 1000 kaydı sakla
                if (changes.Count > 1000)
                {
                    changes = changes.OrderByDescending(c => c.Timestamp).Take(1000).ToList();
                }

                var newJson = JsonSerializer.Serialize(changes, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(logPath, newJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to log change");
            }
        }

        public async Task<ConfigurationBackup?> LoadBackupAsync(string backupId)
        {
            try
            {
                var backupPath = Path.Combine(_basePath, "backups", $"{backupId}.json");

                if (!File.Exists(backupPath))
                    return null;

                var json = await File.ReadAllTextAsync(backupPath);
                return JsonSerializer.Deserialize<ConfigurationBackup>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to load backup: {BackupId}", backupId);
                return null;
            }
        }

        public async Task SaveBackupAsync(ConfigurationBackup backup)
        {
            try
            {
                var backupPath = Path.Combine(_basePath, "backups", $"{backup.Id}.json");
                var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(backupPath, json);

                _logger.LogInformation("[ConfigPersistence] Saved backup: {BackupId}", backup.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to save backup: {BackupId}", backup.Id);
                throw;
            }
        }

        public async Task<IEnumerable<ConfigurationBackup>> LoadBackupsAsync()
        {
            try
            {
                var backupsPath = Path.Combine(_basePath, "backups");
                var backups = new List<ConfigurationBackup>();

                if (!Directory.Exists(backupsPath))
                    return backups;

                var files = Directory.GetFiles(backupsPath, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var backup = JsonSerializer.Deserialize<ConfigurationBackup>(json);

                        if (backup != null)
                        {
                            backups.Add(backup);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[ConfigPersistence] Failed to load backup file: {File}", file);
                    }
                }

                return backups.OrderByDescending(b => b.CreatedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to load backups");
                return Enumerable.Empty<ConfigurationBackup>();
            }
        }

        public async Task DeleteBackupAsync(string backupId)
        {
            try
            {
                var backupPath = Path.Combine(_basePath, "backups", $"{backupId}.json");

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    _logger.LogInformation("[ConfigPersistence] Deleted backup: {BackupId}", backupId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigPersistence] Failed to delete backup: {BackupId}", backupId);
                throw;
            }
        }

        private string GetConfigFilePath(string key)
        {
            var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_basePath, "configs", $"{safeKey}.json");
        }
    }

    /// <summary>
    /// Advanced configuration manager implementasyonu
    /// </summary>
    public class AdvancedConfigurationManager : IAdvancedConfigurationManager
    {
        private readonly IConfigurationPersistence _persistence;
        private readonly ILogger<AdvancedConfigurationManager> _logger;
        private readonly Dictionary<string, ConfigurationEntry> _cache = new();
        private readonly object _cacheLock = new object();
        private DateTime _lastReload = DateTime.UtcNow;

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public AdvancedConfigurationManager(IConfigurationPersistence persistence, ILogger<AdvancedConfigurationManager> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }

        public async Task<T?> GetValueAsync<T>(string key, T? defaultValue = default)
        {
            try
            {
                var entry = await GetEntryAsync(key);

                if (entry?.Value == null)
                {
                    _logger.LogDebug("[ConfigManager] Configuration not found, using default: {Key}", key);
                    return defaultValue;
                }

                if (entry.Value is T directValue)
                {
                    return directValue;
                }

                if (entry.Value is JsonElement jsonElement)
                {
                    return jsonElement.Deserialize<T>();
                }

                if (entry.Value is string stringValue && typeof(T) != typeof(string))
                {
                    return JsonSerializer.Deserialize<T>(stringValue);
                }

                return (T)Convert.ChangeType(entry.Value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to get configuration value: {Key}", key);
                return defaultValue;
            }
        }

        public async Task SetValueAsync<T>(string key, T value, string? userId = null, string? reason = null)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                var existingEntry = await GetEntryAsync(key);
                var oldValue = existingEntry?.Value;

                var entry = existingEntry ?? new ConfigurationEntry
                {
                    Key = key,
                    Metadata = new ConfigurationMetadata { Key = key }
                };

                entry.Value = value;
                entry.Metadata.LastModified = DateTime.UtcNow;
                entry.Metadata.LastModifiedBy = userId;
                entry.Source = "User";

                await _persistence.SaveAsync(entry);

                lock (_cacheLock)
                {
                    _cache[key] = entry;
                }

                // Change event
                var changeEvent = new ConfigurationChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = value,
                    Source = "User",
                    UserId = userId,
                    Reason = reason
                };

                await _persistence.LogChangeAsync(changeEvent);
                ConfigurationChanged?.Invoke(this, changeEvent);

                _logger.LogInformation("[ConfigManager] Configuration updated: {Key}, CorrelationId: {CorrelationId}",
                    key, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to set configuration value: {Key}", key);
                throw;
            }
        }

        public async Task<bool> RemoveAsync(string key, string? userId = null, string? reason = null)
        {
            try
            {
                var existingEntry = await GetEntryAsync(key);

                if (existingEntry == null)
                {
                    return false;
                }

                var success = await _persistence.DeleteAsync(key);

                if (success)
                {
                    lock (_cacheLock)
                    {
                        _cache.Remove(key);
                    }

                    // Change event
                    var changeEvent = new ConfigurationChangedEventArgs
                    {
                        Key = key,
                        OldValue = existingEntry.Value,
                        NewValue = null,
                        Source = "User",
                        UserId = userId,
                        Reason = reason ?? "Removed"
                    };

                    await _persistence.LogChangeAsync(changeEvent);
                    ConfigurationChanged?.Invoke(this, changeEvent);

                    _logger.LogInformation("[ConfigManager] Configuration removed: {Key}", key);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to remove configuration: {Key}", key);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var entry = await GetEntryAsync(key);
            return entry != null;
        }

        public async Task<Dictionary<string, object>> GetAllAsync(string? category = null)
        {
            try
            {
                var allEntries = await _persistence.LoadAllAsync();
                var result = new Dictionary<string, object>();

                foreach (var entry in allEntries)
                {
                    if (category != null && entry.Metadata.Category != category)
                    {
                        continue;
                    }

                    if (entry.Value != null)
                    {
                        result[entry.Key] = entry.Value;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to get all configurations");
                return new Dictionary<string, object>();
            }
        }

        public async Task SetMultipleAsync(Dictionary<string, object> values, string? userId = null, string? reason = null)
        {
            foreach (var kvp in values)
            {
                await SetValueAsync(kvp.Key, kvp.Value, userId, reason);
            }
        }

        public async Task<ConfigurationValidationResult> ValidateAsync(Dictionary<string, object> values)
        {
            var result = new ConfigurationValidationResult { IsValid = true };

            foreach (var kvp in values)
            {
                try
                {
                    var metadata = await GetMetadataAsync(kvp.Key);

                    if (metadata != null)
                    {
                        // Required check
                        if (metadata.IsRequired && kvp.Value == null)
                        {
                            result.Errors.Add($"Configuration '{kvp.Key}' is required");
                            result.IsValid = false;
                            continue;
                        }

                        // Type validation
                        if (!ValidateDataType(kvp.Value, metadata.DataType))
                        {
                            result.Errors.Add($"Configuration '{kvp.Key}' has invalid data type. Expected: {metadata.DataType}");
                            result.IsValid = false;
                            continue;
                        }

                        // Valid values check
                        if (metadata.ValidValues != null && kvp.Value != null)
                        {
                            if (!metadata.ValidValues.Contains(kvp.Value.ToString()))
                            {
                                result.Errors.Add($"Configuration '{kvp.Key}' value is not in valid values list");
                                result.IsValid = false;
                                continue;
                            }
                        }

                        // Range validation
                        if (!ValidateRange(kvp.Value, metadata.MinValue, metadata.MaxValue))
                        {
                            result.Errors.Add($"Configuration '{kvp.Key}' value is out of range");
                            result.IsValid = false;
                            continue;
                        }
                    }

                    result.ValidatedValues[kvp.Key] = kvp.Value;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Validation error for '{kvp.Key}': {ex.Message}");
                    result.IsValid = false;
                }
            }

            return result;
        }

        public async Task<ConfigurationMetadata?> GetMetadataAsync(string key)
        {
            var entry = await GetEntryAsync(key);
            return entry?.Metadata;
        }

        public async Task SetMetadataAsync(string key, ConfigurationMetadata metadata)
        {
            var entry = await GetEntryAsync(key) ?? new ConfigurationEntry { Key = key };
            entry.Metadata = metadata;
            entry.Metadata.Key = key;

            await _persistence.SaveAsync(entry);

            lock (_cacheLock)
            {
                _cache[key] = entry;
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            var allEntries = await _persistence.LoadAllAsync();
            return allEntries.Select(e => e.Metadata.Category).Distinct().OrderBy(c => c);
        }

        public async Task<IEnumerable<ConfigurationEntry>> GetByCategoryAsync(string category)
        {
            var allEntries = await _persistence.LoadAllAsync();
            return allEntries.Where(e => e.Metadata.Category == category);
        }

        public async Task<ConfigurationBackup> CreateBackupAsync(string? description = null, string? userId = null)
        {
            try
            {
                var allConfigs = await GetAllAsync();

                var backup = new ConfigurationBackup
                {
                    CreatedBy = userId ?? "System",
                    Description = description ?? "Automatic backup",
                    Configurations = allConfigs,
                    Metadata = new Dictionary<string, object>
                    {
                        { "ConfigCount", allConfigs.Count },
                        { "CreatedByCorrelationId", MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId ?? string.Empty }
                    }
                };

                await _persistence.SaveBackupAsync(backup);

                _logger.LogInformation("[ConfigManager] Created backup: {BackupId} with {ConfigCount} configurations",
                    backup.Id, allConfigs.Count);

                return backup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to create backup");
                throw;
            }
        }

        public async Task RestoreFromBackupAsync(string backupId, string? userId = null)
        {
            try
            {
                var backup = await _persistence.LoadBackupAsync(backupId);

                if (backup == null)
                {
                    throw new ArgumentException($"Backup not found: {backupId}");
                }

                await SetMultipleAsync(backup.Configurations, userId, $"Restored from backup: {backupId}");

                _logger.LogInformation("[ConfigManager] Restored {ConfigCount} configurations from backup: {BackupId}",
                    backup.Configurations.Count, backupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to restore from backup: {BackupId}", backupId);
                throw;
            }
        }

        public async Task<IEnumerable<ConfigurationBackup>> GetBackupsAsync()
        {
            return await _persistence.LoadBackupsAsync();
        }

        public async Task DeleteBackupAsync(string backupId)
        {
            await _persistence.DeleteBackupAsync(backupId);
        }

        public async Task<IEnumerable<ConfigurationChangedEventArgs>> GetChangeHistoryAsync(string? key = null, DateTime? since = null)
        {
            return await _persistence.GetChangeLogAsync(key, since);
        }

        public async Task<string> EncryptValueAsync(string value)
        {
            // TODO: Encryption implementasyonu
            // Şimdilik base64 encode
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public async Task<string> DecryptValueAsync(string encryptedValue)
        {
            // TODO: Decryption implementasyonu
            // Şimdilik base64 decode
            var bytes = Convert.FromBase64String(encryptedValue);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public async Task EncryptConfigurationAsync(string key)
        {
            var entry = await GetEntryAsync(key);

            if (entry?.Value != null && !entry.Metadata.IsEncrypted)
            {
                var encryptedValue = await EncryptValueAsync(entry.Value.ToString() ?? string.Empty);
                entry.Value = encryptedValue;
                entry.Metadata.IsEncrypted = true;

                await _persistence.SaveAsync(entry);

                lock (_cacheLock)
                {
                    _cache[key] = entry;
                }
            }
        }

        public async Task DecryptConfigurationAsync(string key)
        {
            var entry = await GetEntryAsync(key);

            if (entry?.Value != null && entry.Metadata.IsEncrypted)
            {
                var decryptedValue = await DecryptValueAsync(entry.Value.ToString() ?? string.Empty);
                entry.Value = decryptedValue;
                entry.Metadata.IsEncrypted = false;

                await _persistence.SaveAsync(entry);

                lock (_cacheLock)
                {
                    _cache[key] = entry;
                }
            }
        }

        public async Task<string> ExportToJsonAsync(string? category = null)
        {
            var configs = await GetAllAsync(category);
            return JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task ImportFromJsonAsync(string json, bool overwriteExisting = false, string? userId = null)
        {
            try
            {
                var configs = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (configs != null)
                {
                    foreach (var kvp in configs)
                    {
                        if (!overwriteExisting && await ExistsAsync(kvp.Key))
                        {
                            continue;
                        }

                        await SetValueAsync(kvp.Key, kvp.Value, userId, "Imported from JSON");
                    }
                }

                _logger.LogInformation("[ConfigManager] Imported {ConfigCount} configurations from JSON",
                    configs?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ConfigManager] Failed to import from JSON");
                throw;
            }
        }

        public async Task<byte[]> ExportToBinaryAsync()
        {
            var json = await ExportToJsonAsync();
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public async Task ImportFromBinaryAsync(byte[] data, string? userId = null)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            await ImportFromJsonAsync(json, overwriteExisting: false, userId);
        }

        public async Task ReloadAsync()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
            }

            _lastReload = DateTime.UtcNow;

            _logger.LogInformation("[ConfigManager] Configuration cache reloaded");
        }

        public async Task<bool> IsConfigurationChangedAsync()
        {
            // TODO: File system watcher implementasyonu
            return false;
        }

        private async Task<ConfigurationEntry?> GetEntryAsync(string key)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var cachedEntry))
                {
                    return cachedEntry;
                }
            }

            var entry = await _persistence.LoadAsync(key);

            if (entry != null)
            {
                lock (_cacheLock)
                {
                    _cache[key] = entry;
                }
            }

            return entry;
        }

        private bool ValidateDataType(object? value, string expectedType)
        {
            if (value == null)
                return true;

            return expectedType.ToLower() switch
            {
                "string" => value is string,
                "int" or "integer" => value is int or JsonElement { ValueKind: JsonValueKind.Number },
                "bool" or "boolean" => value is bool or JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False },
                "double" or "decimal" => value is double or decimal or JsonElement { ValueKind: JsonValueKind.Number },
                "datetime" => value is DateTime or DateTimeOffset,
                _ => true
            };
        }

        private bool ValidateRange(object? value, object? minValue, object? maxValue)
        {
            if (value == null || (minValue == null && maxValue == null))
                return true;

            try
            {
                if (value is IComparable comparable)
                {
                    if (minValue != null && comparable.CompareTo(minValue) < 0)
                        return false;

                    if (maxValue != null && comparable.CompareTo(maxValue) > 0)
                        return false;
                }

                return true;
            }
            catch
            {
                return true; // Range validation başarısız, geç
            }
        }
    }
}
