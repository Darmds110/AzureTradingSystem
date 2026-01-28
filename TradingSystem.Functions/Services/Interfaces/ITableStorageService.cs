using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public interface ITableStorageService
{
    Task SaveMarketSchedule(DateTime date, bool isOpen, DateTime? openTime, DateTime? closeTime);
    Task<(bool isOpen, DateTime? openTime, DateTime? closeTime)> GetMarketSchedule(DateTime date);
    Task SaveLatestQuote(string symbol, decimal price, DateTime timestamp);
    Task<(decimal price, DateTime timestamp)?> GetLatestQuote(string symbol);
}