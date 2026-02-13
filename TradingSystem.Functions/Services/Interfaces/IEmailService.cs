namespace TradingSystem.Functions.Services.Interfaces;

/// <summary>
/// Interface for email notification service.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a generic email.
    /// </summary>
    /// <param name="subject">Email subject line</param>
    /// <param name="body">Email body content</param>
    /// <param name="isHtml">Whether the body is HTML formatted</param>
    Task SendEmailAsync(string subject, string body, bool isHtml = false);

    /// <summary>
    /// Send a daily portfolio summary email.
    /// </summary>
    /// <param name="portfolioValue">Current portfolio value</param>
    /// <param name="cashBalance">Current cash balance</param>
    /// <param name="dailyReturnPercent">Today's return percentage</param>
    /// <param name="totalReturnPercent">Total return since inception</param>
    /// <param name="positions">List of current positions with details</param>
    Task SendDailySummaryAsync(
        decimal portfolioValue,
        decimal cashBalance,
        decimal dailyReturnPercent,
        decimal totalReturnPercent,
        IEnumerable<PositionSummary>? positions = null);

    /// <summary>
    /// Send a risk alert email.
    /// </summary>
    /// <param name="alertType">Type of alert (Warning, Critical, etc.)</param>
    /// <param name="message">Alert message</param>
    /// <param name="currentDrawdown">Current drawdown percentage</param>
    Task SendAlertAsync(string alertType, string message, decimal? currentDrawdown = null);

    /// <summary>
    /// Send a trade notification email.
    /// </summary>
    /// <param name="tradeType">BUY or SELL</param>
    /// <param name="symbol">Stock symbol</param>
    /// <param name="quantity">Number of shares</param>
    /// <param name="price">Execution price</param>
    /// <param name="totalValue">Total trade value</param>
    /// <param name="reason">Reason for the trade (strategy name, etc.)</param>
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