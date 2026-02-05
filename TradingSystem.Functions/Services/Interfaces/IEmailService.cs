using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.Functions.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends a general notification email
    /// </summary>
    Task SendNotificationAsync(string subject, string body, string priority = "MEDIUM");

    /// <summary>
    /// Sends a trade execution notification
    /// </summary>
    Task SendTradeNotificationAsync(string symbol, string action, int quantity, decimal price);

    /// <summary>
    /// Sends an error notification
    /// </summary>
    Task SendErrorNotificationAsync(string errorMessage, Exception? exception = null);

    /// <summary>
    /// Sends a daily summary email (simple version)
    /// </summary>
    Task SendDailySummaryAsync(decimal portfolioValue, decimal dayChange, int tradesExecuted);

    /// <summary>
    /// Sends an alert email with priority level
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (plain text or HTML)</param>
    /// <param name="priority">Priority level: CRITICAL, HIGH, MEDIUM, LOW</param>
    Task SendAlertAsync(string subject, string body, string priority);

    /// <summary>
    /// Sends a comprehensive summary email (HTML formatted)
    /// </summary>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML formatted email body</param>
    Task SendSummaryEmailAsync(string subject, string htmlBody);

    /// <summary>
    /// Sends a weekly performance summary
    /// </summary>
    Task SendWeeklySummaryAsync(
        decimal portfolioValue,
        decimal weeklyReturn,
        decimal totalReturn,
        int tradesExecuted,
        decimal winRate);

    /// <summary>
    /// Sends a monthly performance summary
    /// </summary>
    Task SendMonthlySummaryAsync(
        decimal portfolioValue,
        decimal monthlyReturn,
        decimal totalReturn,
        decimal sharpeRatio,
        decimal maxDrawdown,
        int tradesExecuted,
        decimal winRate);
}