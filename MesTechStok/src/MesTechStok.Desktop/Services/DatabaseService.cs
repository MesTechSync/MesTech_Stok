using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// ALPHA TEAM FIX: Use Core DbContext
using MesTechStok.Core.Data;

namespace MesTechStok.Desktop.Services
{
    public interface IDatabaseService
    {
        Task InitializeDatabaseAsync();
        Task<bool> IsDatabaseConnectedAsync();
        Task<string> GetDatabaseInfoAsync();
        Task BackupDatabaseAsync(string backupPath);
        Task RestoreDatabaseAsync(string backupPath);
    }

    /// <summary>
    /// ALPHA TEAM: Database service using Core AppDbContext
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(AppDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation("ALPHA TEAM: Initializing Core database...");

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Apply any pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"ALPHA TEAM: Applying {pendingMigrations.Count()} pending migrations...");
                    await _context.Database.MigrateAsync();
                }

                _logger.LogInformation("ALPHA TEAM: Core database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Failed to initialize Core database");
                throw;
            }
        }

        public async Task<bool> IsDatabaseConnectedAsync()
        {
            try
            {
                await Task.Yield();
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Core database connection check failed");
                return false;
            }
        }

        public async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                await Task.Yield();
                var productCount = await _context.Products.CountAsync();
                var categoryCount = await _context.Categories.CountAsync();
                var movementCount = await _context.StockMovements.CountAsync();
                var orderCount = await _context.Orders.CountAsync();
                var supplierCount = await _context.Suppliers.CountAsync();
                var customerCount = await _context.Customers.CountAsync();

                return $"Products: {productCount}, Categories: {categoryCount}, " +
                       $"StockMovements: {movementCount}, Orders: {orderCount}, " +
                       $"Suppliers: {supplierCount}, Customers: {customerCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Failed to get Core database info");
                return "Core database info unavailable";
            }
        }

        public async Task BackupDatabaseAsync(string backupPath)
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation($"ALPHA TEAM: Creating Core database backup at {backupPath}");

                // For SQLite, we can simply copy the database file
                var dbPath = _context.Database.GetConnectionString();
                if (dbPath != null && dbPath.Contains("Data Source="))
                {
                    var sourceFile = dbPath.Replace("Data Source=", "").Split(';')[0];
                    var backupFile = Path.Combine(backupPath, $"MesTechStok_Core_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                    Directory.CreateDirectory(backupPath);
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, backupFile, true);
                        _logger.LogInformation($"ALPHA TEAM: Core database backup created: {backupFile}");
                    }
                    else
                    {
                        _logger.LogWarning($"ALPHA TEAM: Source database file not found: {sourceFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Core database backup failed");
                throw;
            }
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation($"ALPHA TEAM: Restoring Core database from {backupPath}");

                var dbPath = _context.Database.GetConnectionString();
                if (dbPath != null && dbPath.Contains("Data Source=") && File.Exists(backupPath))
                {
                    var targetFile = dbPath.Replace("Data Source=", "").Split(';')[0];
                    File.Copy(backupPath, targetFile, true);

                    _logger.LogInformation("ALPHA TEAM: Core database restored successfully");
                }
                else
                {
                    _logger.LogWarning($"ALPHA TEAM: Backup file not found: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ALPHA TEAM: Core database restore failed");
                throw;
            }
        }
    }
}