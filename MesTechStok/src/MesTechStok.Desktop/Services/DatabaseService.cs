using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

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
    /// H32: Migrated to Infrastructure.Persistence.AppDbContext.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly InfraDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(InfraDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation("Initializing Infrastructure database...");

                // Apply any pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
                    await _context.Database.MigrateAsync();
                }

                _logger.LogInformation("Infrastructure database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Infrastructure database");
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
                _logger.LogError(ex, "Infrastructure database connection check failed");
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
                _logger.LogError(ex, "Failed to get Infrastructure database info");
                return "Database info unavailable";
            }
        }

        public async Task BackupDatabaseAsync(string backupPath)
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation($"Creating database backup at {backupPath}");

                var dbPath = _context.Database.GetConnectionString();
                if (dbPath != null && dbPath.Contains("Data Source="))
                {
                    var sourceFile = dbPath.Replace("Data Source=", "").Split(';')[0];
                    var backupFile = Path.Combine(backupPath, $"MesTechStok_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                    Directory.CreateDirectory(backupPath);
                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, backupFile, true);
                        _logger.LogInformation($"Database backup created: {backupFile}");
                    }
                    else
                    {
                        _logger.LogWarning($"Source database file not found: {sourceFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup failed");
                throw;
            }
        }

        public async Task RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                await Task.Yield();
                _logger.LogInformation($"Restoring database from {backupPath}");

                var dbPath = _context.Database.GetConnectionString();
                if (dbPath != null && dbPath.Contains("Data Source=") && File.Exists(backupPath))
                {
                    var targetFile = dbPath.Replace("Data Source=", "").Split(';')[0];
                    File.Copy(backupPath, targetFile, true);

                    _logger.LogInformation("Database restored successfully");
                }
                else
                {
                    _logger.LogWarning($"Backup file not found: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database restore failed");
                throw;
            }
        }
    }
}
