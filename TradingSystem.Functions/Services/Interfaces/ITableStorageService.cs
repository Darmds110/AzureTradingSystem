namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for Azure Table Storage operations
    /// </summary>
    public interface ITableStorageService
    {
        /// <summary>
        /// Saves market schedule for a date
        /// </summary>
        Task SaveMarketScheduleAsync(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);

        /// <summary>
        /// Gets market schedule for a date
        /// </summary>
        Task<MarketScheduleEntity?> GetMarketScheduleAsync(DateTime date);

        /// <summary>
        /// Checks if market is currently open
        /// </summary>
        Task<bool> IsMarketOpenAsync();

        /// <summary>
        /// Saves latest quote for a symbol
        /// </summary>
        Task SaveLatestQuoteAsync(string symbol, decimal price, DateTime timestamp);

        /// <summary>
        /// Gets latest quote for a symbol
        /// </summary>
        Task<LatestQuoteEntity?> GetLatestQuoteAsync(string symbol);

        /// <summary>
        /// Sets a cache value with optional expiration
        /// </summary>
        Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Gets a cache value
        /// </summary>
        Task<T?> GetCacheValueAsync<T>(string key);

        /// <summary>
        /// Removes a cache value
        /// </summary>
        Task RemoveCacheValueAsync(string key);
    }

    /// <summary>
    /// Market schedule entity for Table Storage
    /// </summary>
    public class MarketScheduleEntity
    {
        public DateTime Date { get; set; }
        public bool IsOpen { get; set; }
        public DateTime? OpenTime { get; set; }
        public DateTime? CloseTime { get; set; }
    }

    /// <summary>
    /// Latest quote entity for Table Storage
    /// </summary>
    public class LatestQuoteEntity
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}