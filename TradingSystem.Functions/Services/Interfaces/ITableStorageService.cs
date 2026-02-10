namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for Azure Table Storage operations
    /// </summary>
    public interface ITableStorageService
    {
        // Market Schedule methods - both naming conventions for compatibility
        Task SaveMarketScheduleAsync(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);
        Task SaveMarketSchedule(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);

        Task<MarketScheduleResult> GetMarketScheduleAsync(DateTime date);
        Task<MarketScheduleResult> GetMarketSchedule(DateTime date);

        Task<bool> IsMarketOpenAsync();

        // Latest Quote methods - both naming conventions for compatibility
        Task SaveLatestQuoteAsync(string symbol, decimal price, DateTime timestamp);
        Task SaveLatestQuote(string symbol, decimal price, DateTime timestamp);

        Task<LatestQuoteEntity?> GetLatestQuoteAsync(string symbol);

        // Cache methods
        Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetCacheValueAsync<T>(string key);
        Task RemoveCacheValueAsync(string key);
    }

    /// <summary>
    /// Market schedule result tuple
    /// </summary>
    public struct MarketScheduleResult
    {
        public bool isOpen;
        public DateTime? openTime;
        public DateTime? closeTime;

        public MarketScheduleResult(bool isOpen, DateTime? openTime, DateTime? closeTime)
        {
            this.isOpen = isOpen;
            this.openTime = openTime;
            this.closeTime = closeTime;
        }
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