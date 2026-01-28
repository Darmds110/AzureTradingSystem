using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }
}

public interface IMarketDataService
{
    Task<bool> IsMarketOpen();
    Task<DateTime?> GetNextMarketOpen();
    Task<DateTime?> GetNextMarketClose();
    Task UpdateMarketSchedule();
    Task<StockQuote?> GetLatestQuote(string symbol);
    Task<List<StockQuote>> GetHistoricalData(string symbol, DateTime startDate, DateTime endDate);
}