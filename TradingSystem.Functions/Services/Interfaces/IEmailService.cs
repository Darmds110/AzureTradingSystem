namespace TradingSystem.Functions.Services.Interfaces
{
    /// <summary>
    /// Service for sending email notifications
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a generic email
        /// </summary>
        Task SendEmailAsync(string subject, string body);

        /// <summary>
        /// Sends an alert email with priority level
        /// </summary>
        Task SendAlertAsync(string subject, string body, string priority);

        /// <summary>
        /// Sends an error notification email (message only)
        /// </summary>
        Task SendErrorNotificationAsync(string errorMessage);

        /// <summary>
        /// Sends an error notification email with subject and exception
        /// </summary>
        Task SendErrorNotificationAsync(string subject, Exception exception);

        /// <summary>
        /// Sends a summary email
        /// </summary>
        Task SendSummaryEmailAsync(string subject, string body);

        /// <summary>
        /// Sends a weekly performance summary
        /// </summary>
        Task SendWeeklySummaryAsync(decimal portfolioValue, decimal weeklyReturn, decimal totalReturn, int tradesExecuted, decimal winRate);

        /// <summary>
        /// Sends a monthly performance summary
        /// </summary>
        Task SendMonthlySummaryAsync(decimal portfolioValue, decimal monthlyReturn, decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int tradesExecuted, decimal azureCosts);
    }
}