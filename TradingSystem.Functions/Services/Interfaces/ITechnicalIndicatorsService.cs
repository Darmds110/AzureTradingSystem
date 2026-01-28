using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public class TechnicalIndicators
{
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? EMA12 { get; set; }
    public decimal? EMA26 { get; set; }
    public decimal? RSI { get; set; }
    public decimal? MACD { get; set; }
    public decimal? MACDSignal { get; set; }
    public decimal? MACDHistogram { get; set; }
}

public interface ITechnicalIndicatorsService
{
    Task<TechnicalIndicators> CalculateIndicators(string symbol, DateTime asOfDate);
}