namespace TradingSystem.Functions.Services.Interfaces;

/// <summary>
/// Interface for email notification service using Azure Communication Services.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a generic email.
    /// </summary>
    Task SendEmailAsync(string subject, string body, bool isHtml = false);

    /// <summary>
    /// Send a summary email with custom subject and HTML body.
    /// Used by DailyPortfolioSummary function.
    /// </summary>
    Task SendSummaryEmailAsync(string subject, string htmlBody);

    /// <summary>
    /// Send a daily portfolio summary email.
    /// </summary>
    Task SendDailySummaryAsync(
        decimal portfolioValue,
        decimal cashBalance,
        decimal dailyReturnPercent,
        decimal totalReturnPercent,
        IEnumerable<PositionSummary>? positions = null);

    /// <summary>
    /// Send a weekly portfolio summary email.
    /// </summary>
    Task SendWeeklySummaryAsync(
        decimal portfolioValue,
        decimal weeklyReturnPercent,
        decimal totalReturnPercent,
        int tradesThisWeek,
        decimal winRate);

    /// <summary>
    /// Send a monthly portfolio summary email.
    /// </summary>
    Task SendMonthlySummaryAsync(
        decimal portfolioValue,
        decimal monthlyReturnPercent,
        decimal totalReturnPercent,
        decimal sharpeRatio,
        decimal maxDrawdownPercent,
        int tradesThisMonth,
        decimal azureCosts);

    /// <summary>
    /// Send a risk/system alert email.
    /// </summary>
    /// <param name="title">Alert title</param>
    /// <param name="message">Alert message body</param>
    /// <param name="priority">Priority level: "CRITICAL", "HIGH", "MEDIUM", "LOW"</param>
    Task SendAlertAsync(string title, string message, string priority);

    /// <summary>
    /// Send an error notification email (single message).
    /// </summary>
    Task SendErrorNotificationAsync(string errorMessage);

    /// <summary>
    /// Send an error notification email with exception details.
    /// </summary>
    Task SendErrorNotificationAsync(string errorMessage, Exception exception);

    /// <summary>
    /// Send a trade notification email.
    /// </summary>
    Task SendTradeNotificationAsync(
        string tradeType,
        string symbol,
        int quantity,
        decimal price,
        decimal totalValue,
        string? reason = null);
}

/// <summary>
/// Summary information for a position, used in email reports.
/// </summary>
public class PositionSummary
{
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPL { get; set; }
    public decimal UnrealizedPLPercent { get; set; }
    public int DaysHeld { get; set; }
}