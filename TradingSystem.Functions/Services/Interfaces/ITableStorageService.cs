using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public interface ITableStorageService
{
    // Market Schedule methods
    Task SaveMarketScheduleAsync(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);
    Task<(bool isOpen, DateTime? openTime, DateTime? closeTime)> GetMarketScheduleAsync(DateTime date);

    /// <summary>
    /// Quick check if market is currently open
    /// </summary>
    Task<bool> IsMarketOpenAsync();

    // Quote caching methods
    Task SaveLatestQuoteAsync(string symbol, decimal price, DateTime timestamp);
    Task<(decimal price, DateTime timestamp)?> GetLatestQuoteAsync(string symbol);

    // Generic caching methods for temporary values
    /// <summary>
    /// Sets a cached value with optional expiration
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Gets a cached value
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default</returns>
    Task<T> GetCacheValueAsync<T>(string key);

    /// <summary>
    /// Removes a cached value
    /// </summary>
    /// <param name="key">Cache key</param>
    Task RemoveCacheValueAsync(string key);

    // Legacy methods (without Async suffix for backwards compatibility)
    [Obsolete("Use SaveMarketScheduleAsync instead")]
    Task SaveMarketSchedule(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);

    [Obsolete("Use GetMarketScheduleAsync instead")]
    Task<(bool isOpen, DateTime? openTime, DateTime? closeTime)> GetMarketSchedule(DateTime date);

    [Obsolete("Use SaveLatestQuoteAsync instead")]
    Task SaveLatestQuote(string symbol, decimal price, DateTime timestamp);

    [Obsolete("Use GetLatestQuoteAsync instead")]
    Task<(decimal price, DateTime timestamp)?> GetLatestQuote(string symbol);
}