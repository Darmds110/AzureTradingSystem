namespace TradingSystem.Functions.Services.Interfaces;

/// <summary>
/// Interface for email notification service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a generic email.
    /// </summary>
    Task SendEmailAsync(string subject, string body, bool isHtml = false);

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
        decimal weeklyProfitLoss);

    /// <summary>
    /// Send a monthly portfolio summary email.
    /// </summary>
    Task SendMonthlySummaryAsync(
        decimal portfolioValue,
        decimal monthlyReturnPercent,
        decimal totalReturnPercent,
        int tradesThisMonth,
        decimal monthlyProfitLoss,
        decimal azureCosts);

    /// <summary>
    /// Send a generic summary email (used by DailyPortfolioSummary function).
    /// </summary>
    Task SendSummaryEmailAsync(string subject, string htmlBody);

    /// <summary>
    /// Send a risk alert email.
    /// </summary>
    /// <param name="alertType">Type of alert (Warning, Critical, etc.)</param>
    /// <param name="message">Alert message</param>
    /// <param name="currentDrawdown">Current drawdown percentage (optional)</param>
    Task SendAlertAsync(string alertType, string message, decimal? currentDrawdown = null);

    /// <summary>
    /// Send an error notification email.
    /// </summary>
    /// <param name="functionName">Name of the function that failed</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="exception">Optional exception details</param>
    Task SendErrorNotificationAsync(string functionName, string errorMessage, Exception? exception = null);

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