using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public interface IEmailService
{
    Task SendNotificationAsync(string subject, string body, string priority = "MEDIUM");
    Task SendTradeNotificationAsync(string symbol, string action, int quantity, decimal price);
    Task SendErrorNotificationAsync(string errorMessage, Exception? exception = null);
    Task SendDailySummaryAsync(decimal portfolioValue, decimal dayChange, int tradesExecuted);
}