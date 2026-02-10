using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TradingSystem.Functions.Config;
using TradingSystem.Functions.Services.Interfaces;

namespace TradingSystem.Functions.Services
{
    /// <summary>
    /// Email service implementation using MailKit
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

        public async Task SendEmailAsync(string subject, string body)
        {
            await SendEmailInternalAsync(subject, body);
        }

        public async Task SendAlertAsync(string subject, string body, string priority)
        {
            var priorityPrefix = priority.ToUpper() switch
            {
                "CRITICAL" => "🚨 CRITICAL: ",
                "HIGH" => "⚠️ ",
                "MEDIUM" => "ℹ️ ",
                _ => ""
            };

            var fullSubject = $"{priorityPrefix}{subject}";
            var fullBody = $"Priority: {priority}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n{body}";

            await SendEmailInternalAsync(fullSubject, fullBody);
        }

        public async Task SendErrorNotificationAsync(string errorMessage)
        {
            var subject = "❌ Trading System Error";
            var body = $"An error occurred in the Trading System.\n\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nError:\n{errorMessage}";
            await SendEmailInternalAsync(subject, body);
        }

        public async Task SendErrorNotificationAsync(string subject, Exception exception)
        {
            var fullSubject = $"❌ Error: {subject}";
            var body = $"An error occurred in the Trading System.\n\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nError: {exception.Message}\n\nStack Trace:\n{exception.StackTrace}";
            await SendEmailInternalAsync(fullSubject, body);
        }

        public async Task SendSummaryEmailAsync(string subject, string body)
        {
            await SendEmailInternalAsync(subject, body);
        }

        public async Task SendWeeklySummaryAsync(
            decimal portfolioValue,
            decimal weeklyReturn,
            decimal totalReturn,
            int tradesExecuted,
            decimal winRate)
        {
            var subject = $"📊 Weekly Trading Summary - {DateTime.UtcNow:MMM dd, yyyy}";

            var body = $@"
Weekly Performance Summary
==========================

Portfolio Value: ${portfolioValue:N2}
Weekly Return: {weeklyReturn:F2}%
Total Return: {totalReturn:F2}%

Trading Activity:
- Trades Executed: {tradesExecuted}
- Win Rate: {winRate:F1}%

Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
";

            await SendEmailInternalAsync(subject, body);
        }

        public async Task SendMonthlySummaryAsync(
            decimal portfolioValue,
            decimal monthlyReturn,
            decimal totalReturn,
            decimal sharpeRatio,
            decimal maxDrawdown,
            int tradesExecuted,
            decimal azureCosts)
        {
            var subject = $"📈 Monthly Trading Report - {DateTime.UtcNow:MMMM yyyy}";

            var body = $@"
Monthly Performance Report
==========================

Portfolio Summary:
- Current Value: ${portfolioValue:N2}
- Monthly Return: {monthlyReturn:F2}%
- Total Return (All-Time): {totalReturn:F2}%

Risk Metrics:
- Sharpe Ratio: {sharpeRatio:F2}
- Maximum Drawdown: {maxDrawdown:F2}%

Trading Activity:
- Total Trades: {tradesExecuted}

Cost Analysis:
- Azure Costs: ${azureCosts:N2}
- Net Return: ${portfolioValue - 1000 - azureCosts:N2}

Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
";

            await SendEmailInternalAsync(subject, body);
        }

        private async Task SendEmailInternalAsync(string subject, string body)
        {
            try
            {
                _logger.LogInformation("Sending email: {subject}", subject);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Trading System", _config.FromAddress));
                message.To.Add(new MailboxAddress("", _config.ToAddress));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();

                await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config.SmtpUsername, _config.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully: {subject}", subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {subject}", subject);
                throw;
            }
        }
    }
}