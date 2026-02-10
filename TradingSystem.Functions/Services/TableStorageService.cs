using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Azure Table Storage service implementation
    /// </summary>
    public class TableStorageService : ITableStorageService
    {
        private readonly TableClient _marketScheduleTable;
        private readonly TableClient _latestQuotesTable;
        private readonly TableClient _cacheTable;
        private readonly ILogger<TableStorageService> _logger;

        public TableStorageService(StorageConfig config, ILogger<TableStorageService> logger)
        {
            _logger = logger;

            var serviceClient = new TableServiceClient(config.ConnectionString);

            _marketScheduleTable = serviceClient.GetTableClient(config.MarketScheduleTableName);
            _marketScheduleTable.CreateIfNotExists();

            _latestQuotesTable = serviceClient.GetTableClient(config.LatestQuotesTableName);
            _latestQuotesTable.CreateIfNotExists();

            _cacheTable = serviceClient.GetTableClient("Cache");
            _cacheTable.CreateIfNotExists();
        }

        /// <summary>
        /// Saves market schedule for a date
        /// </summary>
        public async Task SaveMarketScheduleAsync(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime)
        {
            var entity = new TableEntity("MarketSchedule", date.ToString("yyyy-MM-dd"))
            {
                { "IsOpen", isOpen },
                { "OpenTime", openTime?.ToString("o") ?? "" },
                { "CloseTime", closeTime?.ToString("o") ?? "" },
                { "UpdatedAt", DateTime.UtcNow }
            };

            await _marketScheduleTable.UpsertEntityAsync(entity);
            _logger.LogDebug("Saved market schedule for {date}: IsOpen={isOpen}", date, isOpen);
        }

        /// <summary>
        /// Gets market schedule for a date
        /// </summary>
        public async Task<MarketScheduleEntity?> GetMarketScheduleAsync(DateTime date)
        {
            try
            {
                var response = await _marketScheduleTable.GetEntityAsync<TableEntity>(
                    "MarketSchedule", date.ToString("yyyy-MM-dd"));

                var entity = response.Value;
                return new MarketScheduleEntity
                {
                    Date = date,
                    IsOpen = entity.GetBoolean("IsOpen") ?? false,
                    OpenTime = DateTime.TryParse(entity.GetString("OpenTime"), out var open) ? open : null,
                    CloseTime = DateTime.TryParse(entity.GetString("CloseTime"), out var close) ? close : null
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if market is currently open
        /// </summary>
        public async Task<bool> IsMarketOpenAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var schedule = await GetMarketScheduleAsync(today);
            if (schedule == null || !schedule.IsOpen)
            {
                return false;
            }

            // Check if current time is within market hours
            if (schedule.OpenTime.HasValue && schedule.CloseTime.HasValue)
            {
                return now >= schedule.OpenTime.Value && now <= schedule.CloseTime.Value;
            }

            // Default NYSE hours: 9:30 AM - 4:00 PM ET
            var etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var etNow = TimeZoneInfo.ConvertTimeFromUtc(now, etZone);

            var marketOpen = new TimeSpan(9, 30, 0);
            var marketClose = new TimeSpan(16, 0, 0);

            return etNow.TimeOfDay >= marketOpen && etNow.TimeOfDay <= marketClose;
        }

        /// <summary>
        /// Saves latest quote for a symbol
        /// </summary>
        public async Task SaveLatestQuoteAsync(string symbol, decimal price, DateTime timestamp)
        {
            var entity = new TableEntity("Quotes", symbol)
            {
                { "Price", (double)price },
                { "Timestamp", timestamp },
                { "UpdatedAt", DateTime.UtcNow }
            };

            await _latestQuotesTable.UpsertEntityAsync(entity);
            _logger.LogDebug("Saved quote for {symbol}: ${price}", symbol, price);
        }

        /// <summary>
        /// Gets latest quote for a symbol
        /// </summary>
        public async Task<LatestQuoteEntity?> GetLatestQuoteAsync(string symbol)
        {
            try
            {
                var response = await _latestQuotesTable.GetEntityAsync<TableEntity>("Quotes", symbol);
                var entity = response.Value;

                return new LatestQuoteEntity
                {
                    Symbol = symbol,
                    Price = (decimal)(entity.GetDouble("Price") ?? 0),
                    Timestamp = entity.GetDateTime("Timestamp") ?? DateTime.MinValue
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets a cache value with optional expiration
        /// </summary>
        public async Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(value);
            var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;

            var entity = new TableEntity("Cache", key)
            {
                { "Value", json },
                { "ExpiresAt", expiresAt },
                { "CreatedAt", DateTime.UtcNow }
            };

            await _cacheTable.UpsertEntityAsync(entity);
            _logger.LogDebug("Cached value for key: {key}", key);
        }

        /// <summary>
        /// Gets a cache value
        /// </summary>
        public async Task<T?> GetCacheValueAsync<T>(string key)
        {
            try
            {
                var response = await _cacheTable.GetEntityAsync<TableEntity>("Cache", key);
                var entity = response.Value;

                // Check expiration
                var expiresAt = entity.GetDateTime("ExpiresAt");
                if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
                {
                    // Expired - remove and return default
                    await RemoveCacheValueAsync(key);
                    return default;
                }

                var json = entity.GetString("Value");
                if (string.IsNullOrEmpty(json))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(json);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return default;
            }
        }

        /// <summary>
        /// Removes a cache value
        /// </summary>
        public async Task RemoveCacheValueAsync(string key)
        {
            try
            {
                await _cacheTable.DeleteEntityAsync("Cache", key);
                _logger.LogDebug("Removed cache value for key: {key}", key);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Already deleted
            }
        }
    }
}