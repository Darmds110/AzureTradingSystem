using Microsoft.Extensions.Logging;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Email service implementation - TESTING MODE (logs instead of sending)
    /// TODO: Replace with SendGrid or OAuth2 for production
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailConfig config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task SendEmailAsync(string subject, string body)
        {
            LogEmail("EMAIL", subject, body);
            return Task.CompletedTask;
        }

        public Task SendAlertAsync(string subject, string body, string priority)
        {
            var priorityEmoji = priority.ToUpper() switch
            {
                "CRITICAL" => "🚨",
                "HIGH" => "⚠️",
                "MEDIUM" => "ℹ️",
                _ => "📧"
            };

            LogEmail($"ALERT [{priority}]", $"{priorityEmoji} {subject}", body);
            return Task.CompletedTask;
        }

        public Task SendErrorNotificationAsync(string errorMessage)
        {
            LogEmail("ERROR", "Trading System Error", errorMessage);
            return Task.CompletedTask;
        }

        public Task SendErrorNotificationAsync(string subject, Exception exception)
        {
            var body = $"Error: {exception.Message}\n\nStack Trace:\n{exception.StackTrace}";
            LogEmail("ERROR", $"❌ {subject}", body);
            return Task.CompletedTask;
        }

        public Task SendSummaryEmailAsync(string subject, string body)
        {
            LogEmail("SUMMARY", subject, body);
            return Task.CompletedTask;
        }

        public Task SendWeeklySummaryAsync(
            decimal portfolioValue,
            decimal weeklyReturn,
            decimal totalReturn,
            int tradesExecuted,
            decimal winRate)
        {
            var body = $@"
Weekly Performance Summary
==========================
Portfolio Value: ${portfolioValue:N2}
Weekly Return: {weeklyReturn:F2}%
Total Return: {totalReturn:F2}%
Trades Executed: {tradesExecuted}
Win Rate: {winRate:F1}%
";
            LogEmail("WEEKLY SUMMARY", $"📊 Weekly Trading Summary", body);
            return Task.CompletedTask;
        }

        public Task SendMonthlySummaryAsync(
            decimal portfolioValue,
            decimal monthlyReturn,
            decimal totalReturn,
            decimal sharpeRatio,
            decimal maxDrawdown,
            int tradesExecuted,
            decimal azureCosts)
        {
            var body = $@"
Monthly Performance Report
==========================
Portfolio Value: ${portfolioValue:N2}
Monthly Return: {monthlyReturn:F2}%
Total Return: {totalReturn:F2}%
Sharpe Ratio: {sharpeRatio:F2}
Max Drawdown: {maxDrawdown:F2}%
Trades Executed: {tradesExecuted}
Azure Costs: ${azureCosts:N2}
";
            LogEmail("MONTHLY SUMMARY", $"📈 Monthly Trading Report", body);
            return Task.CompletedTask;
        }

        private void LogEmail(string type, string subject, string body)
        {
            _logger.LogInformation(
                "\n" +
                "╔══════════════════════════════════════════════════════════════╗\n" +
                "║ {Type,-60} ║\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ TO: {To,-56} ║\n" +
                "║ SUBJECT: {Subject,-51} ║\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "{Body}\n" +
                "╚══════════════════════════════════════════════════════════════╝",
                type,
                _config.ToAddress ?? "not-configured",
                subject.Length > 51 ? subject[..48] + "..." : subject,
                body);
        }
    }
}